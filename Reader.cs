﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
#if DEBUG
using Parse;
#else
using Crunch.Machine;
#endif

#if !DEBUG
namespace Crunch
{
    public class Reader : Machine.Reader
    {
        private static Func<LinkedListNode<object>, LinkedListNode<object>> NextOperand = (node) =>
        {
            //Next node is a minus sign
            if (node.Next != null && node.Next.Value == subtract)
            {
                //Delete the negative sign
                node.List.Remove(node.Next);

                //Negate what's after
                node.Next.Value = negator(node.Next.Value);
            }

            return node.Next;
        };

        private static Func<LinkedListNode<object>, LinkedListNode<object>> PreviousOrZero = (node) =>
        {
            if (node.Previous == null)
            {
                node.List.AddBefore(node, 0);
            }

            return node.Previous;
        };

        //internal static Reader Instance => instance ?? (instance = new Reader());
        //private static Reader instance;

        //protected override Trie<Operator> Operations => operations;
        //private Trie<Operator> operations;

        private static Func<object, object> negator = (o) => //Operand.Multiply(o.ParseOperand(), -1);
        {
            Operand temp = ParseOperand(o);
            temp.Multiply(-1);
            return temp;
        };
        //private static Operator exponentiate = BinaryOperator(Operand.Exponentiate);
        private static Operator exponentiate = BinaryOperator((o1, o2) => o1.Exponentiate(o2));
        private static Operator subtract = new Operator((o) =>
        {
            Operand ans = ParseOperand(o[0]);
            ans.Subtract(ParseOperand(o[1]));
            return ans;
        }, PreviousOrZero, NextOperand);

        public Reader(params KeyValuePair<string, Operator>[][] data) : base(data) { }
        private static Reader Instance;

        static Reader()
        {
            Instance = new Reader(
                new KeyValuePair<string, Operator>[]
                {
                    new KeyValuePair<string, Operator>("sin", Trig("sin", System.Math.Sin, System.Math.Asin)),
                    new KeyValuePair<string, Operator>("cos", Trig("cos", System.Math.Cos, System.Math.Acos)),
                    new KeyValuePair<string, Operator>("tan", Trig("tan", System.Math.Tan, System.Math.Atan)),
                    Function.MachineInstructions("log_", 2, (o) => System.Math.Log(o[1], o[0])),
                    Function.MachineInstructions("log", 1, (o) => System.Math.Log(o[0], Math.ImplicitLogarithmBase)),
                    Function.MachineInstructions("ln", 1, (o) => System.Math.Log(o[0], System.Math.E)),
                    Function.MachineInstructions("sqrt", 1, (o) => System.Math.Pow(o[0], 0.5))
                },
                new KeyValuePair<string, Operator>[]
                {
                    new KeyValuePair<string, Operator>("^", exponentiate)
                },
                new KeyValuePair<string, Operator>[]
                {
                    new KeyValuePair<string, Operator>("/", BinaryOperator((o1, o2) => o1.Divide(o2))),
                    new KeyValuePair<string, Operator>("*", BinaryOperator((o1, o2) => o1.Multiply(o2)))
                },
                new KeyValuePair<string, Operator>[]
                {
                    new KeyValuePair<string, Operator>("-", subtract),
                    new KeyValuePair<string, Operator>("+", BinaryOperator((o1, o2) => o1.Add(o2)))
                }
                );
        }

        private static bool isInverse;

        private static Operator Trig(string name, Func<double, double> normal, Func<double, double> inverse)
        {
            Func<LinkedListNode<object>, LinkedListNode<object>> next = (node) =>
            {
                isInverse = false;

                if (node.Next.Value == exponentiate)
                {
                    Operand e = ParseOperand(node.Next.Next.Value);

                    if (e != null && e.IsConstant(-1))
                    //if ((node + 2).IsEqualTo("-") && (node + 3).IsEqualTo("1"))
                    {
                        node.Next.Next.Value = 1;

                        isInverse = true;
                    }

                    return node.Next.Next.Next;
                }
                else
                {
                    return node.Next;
                }
            };

            return new Operator(
                (o) =>
                {
                    //Expression e = o[0].ParseOperand();
                    //double d;
                    //return e.IsConstant(out d) ? new Term(f(d)) : new Term(new Function(name, e, null));

                    Func<Trigonometry, double, double> temp = (trig, d) => normal(trig == Trigonometry.Degrees ? d * System.Math.PI / 180 : d);

                    if (isInverse)
                    {
                        temp = (trig, d) =>
                        {
                            double value = inverse(d);
                            if (double.IsNaN(value))
                            {

                            }
                            return value * (trig == Trigonometry.Degrees ? 180 / System.Math.PI : 1);
                        };
                    }

                    return new TrigFunction(name + (isInverse ? "^-1" : ""), ParseOperand(o[0]), temp);
                },
                next);
        }

        private static Operator BinaryOperator(Action<Operand, Operand> operation) => new BinaryOperator((o1, o2) =>
        {
            Operand ans = ParseOperand(o1);
            operation(ans, ParseOperand(o2));
            return ans;
        }, (node) => node.Previous, NextOperand);

        public static Operand Evaluate(string str)
        {
            Print.Log("evaluating " + str);
            try
            {
                return ParseOperand(Instance.Parse(str));
            }
            catch (Exception e)
            {
                Print.Log("error evaluating", e.Message);
                return null;
            }
        }

        internal static Operand ParseOperand(object str)
        {
            while (str is LinkedList<object>)
            {
                str = (str as LinkedList<object>).First?.Value;
            }

            if (str == null)
            {
                throw new Exception("Incorrectly formatted math");
            }
            if (str is Operand || str is Expression || str is Term)
            {
                return str as dynamic;
            }
            if (str is Function)
            {
                return new Term(str as Function);
            }

            string s = str.ToString();

            if (s.Substring(0, 1).IsNumber())
            {
                return new Term(s);
            }
            else
            {
                if (s.Length > 1 || str is Operator) // Instance.Operations.Contains(s).ToBool())
                {
                    throw new Exception("Cannot operate on value " + str + " of type " + str.GetType());
                }

                return new Term(s[0]);
            }
        }
    }
}
#endif

#if DEBUG
namespace Crunch
{
    using Operator = Operator<Operand>;

    public class Reader : CharReader<Operand>
    {
        public Reader(Trie<Tuple<Operator, int>> operations) : base(operations) { }

        protected override Operand ParseOperand(string operand) => Term.Parse(operand);

        protected override IEnumerable<string> Segment(IEnumerable<char> operand)
        {
            IEnumerator<char> itr = operand.GetEnumerator();
            string number = "";

            while (itr.MoveNext())
            {
                if (Machine.StringClassification.IsNumber(itr.Current.ToString()))
                {
                    number += itr.Current;
                }
                else
                {
                    if (number.Length > 0)
                    {
                        yield return number;
                        number = "";
                    }

                    yield return itr.Current.ToString();
                }
            }

            if (number.Length > 0)
            {
                yield return number;
            }
        }

        protected override Operand Juxtapose(IEnumerable<Operand> expression)
        {
            Operand ans = 1;

            foreach (Operand o in expression)
            {
                ans.Multiply(o);
            }

            return ans;
        }

        private static bool Sequence(IEnumerator<object> itr, params Func<object, bool>[] comparers)
        {
            int i = 0;
            while (i < comparers.Length && itr.MoveNext())
            {
                if (!comparers[i++](itr.Current))
                {
                    return false;
                }
            }

            return true;
        }

        public static Operator Trig(string name, Func<double, double> normal, Func<double, double> inverse)
        {
            //TrigFunction trig = new TrigFunction(name + (isInverse ? "^-1" : ""), null, normal, inverse);
            UnaryOperator<Operand> trig = new UnaryOperator<Operand>(null, null);

            Action<IEditEnumerator<object>> next = (itr) =>
            {
                trig.Operate = (o) => new Term(new TrigFunction(name, o[0], normal, inverse));

                if (itr.MoveNext() && itr.Current.Equals("^"))
                {
                    int remove = 0;

                    if (Sequence(itr.Copy(), (o) => o.Equals("-"), (o) => o.Equals("1"), (o) => Math.MathReader.Classify(o) == Member.Opening))
                    {
                        remove = 3;
                    }
                    else if (Sequence(itr.Copy(), (o) => Math.MathReader.Classify(o) == Member.Opening, (o) => o.Equals("-"), (o) => o.Equals("1"), (o) => Math.MathReader.Classify(o) == Member.Closing))
                    {
                        remove = 5;
                    }

                    if (remove == 0)
                    {
                        itr.MoveNext();
                        itr.Add(0, Math.MathReader.ParseOperand(itr));
                        itr.MoveNext();
                    }
                    else
                    {
                        itr.Move(remove);
                        for (int i = 0; i < remove; i++)
                        {
                            itr.Remove(-1);
                        }

                        trig.Operate = (o) => new Term(new TrigFunction("arc" + name, o[0], normal, inverse) { IsInverse = true });
                    }

                    /*IEditEnumerator<object> temp = itr.Copy();
                    string nextFew = "";
                    bool isMinus1 = false;
                    if (temp.MoveNext() && Math.MathReader.Classify(itr.Current) == Member.Opening)
                    {

                    }
                    else if (temp.Current.Equals("-"))
                    {
                        Print.Log("a");
                        if (temp.MoveNext() && temp.Current.Equals("1"))
                        {
                            Print.Log("b");
                            if (temp.MoveNext() && Math.MathReader.Classify(temp.Current) == Member.Opening)
                            {
                                Print.Log("c");
                                trig.Operate = (o) => new Term(new TrigFunction("arc" + name, o[0], normal, inverse) { IsInverse = true });
                                temp.Remove(-2);
                            }
                        }
                    }*/

                    /*Operand e = Math.MathReader.ParseOperand(itr);

                    if (e != null && e.IsConstant(-1))
                    //if ((node + 2).IsEqualTo("-") && (node + 3).IsEqualTo("1"))
                    {
                        itr.Move(-1);
                        itr.Remove(1);
                        itr.Add(1, new Operand(new Term(1)));
                        itr.MoveNext();

                        isInverse = true;
                    }*/
                }
            };

            trig.Targets = new Action<IEditEnumerator<object>>[] { next };
            return trig;

            /*return new UnaryOperator<Operand>(
                (o) =>
                {
                    //Expression e = o[0].ParseOperand();
                    //double d;
                    //return e.IsConstant(out d) ? new Term(f(d)) : new Term(new Function(name, e, null));

                    Func<double, double> temp = (d) => normal(d);// normal(trig == Trigonometry.Degrees ? d * System.Math.PI / 180 : d);

                    if (isInverse)
                    {
                        temp = (d) =>
                        {
                            double value = inverse(d);
                            if (double.IsNaN(value))
                            {

                            }
                            return value;// * (trig == Trigonometry.Degrees ? 180 / System.Math.PI : 1);
                        };
                    }

                    //trig.Operands = new Operand[] { o };
                    return new Term(new TrigFunction(name + (isInverse ? "^-1" : ""), o, normal, inverse));
                },
                next);*/
        }

        public class BinaryOperator : BinaryOperator<Operand>
        {
            public BinaryOperator(Action<Operand, Operand> operation, Action<IEditEnumerator<object>> prev = null, Action<IEditEnumerator<object>> next = null, bool juxtapose = false) : base(
                (o1, o2) =>
                {
                    operation(o1, o2);
                    return o1;
                },
                (itr) => Move(prev ?? Prev, itr, juxtapose ? -1 : 0),
                (itr) => Move(next ?? Next, itr, juxtapose ? 1 : 0))
            { }

            private static void Move(Action<IEditEnumerator<object>> mover, IEditEnumerator<object> itr, int juxtapose)
            {
                mover(itr);

                if (juxtapose != 0)
                {
                    // itr is pointing at the thing we ultimately want to return - we need to move off it to make sure it's juxtaposed too
                    itr.Move(-juxtapose);
                    // Juxtapose will delete the thing we were just on - add the result where it was
                    itr.Add(-juxtapose, Math.MathReader.Juxtapose(Math.MathReader.CollectOperands(itr, (ProcessingOrder)juxtapose)));
                    // Move back to where we were (which is now the juxtaposed value)
                    itr.Move(-juxtapose);
                }
            }

            public static void Prev(IEditEnumerator<object> itr) => itr.MovePrev();

            public static void Next(IEditEnumerator<object> itr)
            {
                // Is the next thing a minus sign?
                if (itr.MoveNext() && itr.Current.Equals("-"))
                {
                    // Move off the negative sign (to the thing after)
                    itr.MoveNext();
                    // Delete the negative sign
                    itr.Remove(-1);

                    // Negate the value after
                    Operand next = Math.MathReader.ParseOperand(itr);
                    next.Multiply(-1);
                    // Replace with the negated value
                    itr.Add(0, next);
                }
            }
        }
    }
}
#endif