namespace Sharper.C.Printers
{
    public interface IPrinterSink
    {
        void Print(string s);
    }

    public static class Sink
    {
        public static IPrinterSink Anonymous(Action<string> print)
        {
            return new AnonymousSink(print);
        }

        private struct AnonymousSink : IPrinterSink
        {
            private readonly Action<string> print;

            public Anonymous(Action<string> print)
            {
                this.print = print;
            }

            public void Print(string s)
            {
                print(s);
            }
        }
    }

    public sealed class TextSink : IPrinterSink
    {
        private readonly TextWriter writer;

        private TextSink(TextWriter writer)
        {
            this.writer = writer;
        }

        public static TextSink Create(TextWriter writer)
        {
            return new TextSink(writer);
        }

        public void Print(string s)
        {
            writer.Write(s);
        }
    }

    public sealed class JsonSink : IPrinterSink
    {
        private readonly TextWriter writer;

        private JsonSink(TextWriter writer)
        {
            this.writer = writer;
        }

        public static JsonSink Create(TextWriter writer)
        {
            return new JsonSink(writer);
        }

        public void Print(string s)
        {
            writer.Write(s);
        }
    }

    public struct Printer<S>
        where S : IPrinterSink
    {
        internal readonly Action<S> print;

        internal Printer(Action<S> print)
        {
            this.print = print;
        }

        public void Run(S sink)
        {
            print(sink);
        }
    }

    public struct Print<S>
        where S : IPrinterSink
    {
        internal Printer<S> Printer(Action<S> print)
        {
            return new Printer<S>(print);
        }

        internal Printer<S> String(string s)
        {
            return Print.Printer(sk => sk.Write(s));
        }

        internal Printer<S> Number(int n)
        {
            return String(n.ToString());
        }

        public Printer<S> Sequence(IEnumerable<Printer<S>> ps)
        {
            return Printer(sk => { foreach (var p in ps) p.print(sk); });
        }

        public Printer<S> SequenceArgs(params Printer<S>[] ps)
        {
            return Sequence(ps);
        }

        public Printer<S> Intersperse(
                Printer<S> sep,
                IEnumerable<Printer<S>> ps)
        {
            return Sequence(ps.SelectMany(p => new[] {sep, p}).Skip(1));
        }

        public Printer<S> Bracket(
                Printer<S> begin,
                Printer<S> end,
                Printer<S> p)
        {
            return SequenceArgs(begin, p, end);
        }
    }

    public struct PrintText
    {
        public readonly Print<TextSink> Print = default(Print<TextSink>);
    }

    public struct PrintJson
    {
        public readonly Print<JsonSink> Print = default(Print<JsonSink>);

        public Printer<JsonSink> Unescaped(string s)
        {
            return Print.String(s);
        }

        public Printer<JsonSink> Escape<S>(Printer<S> p)
        {
            return Print.Printer(
                    sk => p.Run(
                            Sink.Anonymous(
                                    s => sk.Write(JsonEscape(s)))));
        }

        public Printer<JsonSink> Array(IEnumerable<Printer<JsonSink>> ps)
        {
            return Print.Bracket("[", "]", Print.Intersperse(",", ps));
        }

        public Printer<JsonSink> Object(
                IEnumerable<KeyValuePair<string, Printer<JsonSink>>> ps)
        {
            return Print.Bracket(
                    "{",
                    "}",
                    Print.Intersperse(
                            ",",
                            ps.Unique().Select(
                                    p => Print.Sequence(
                                            String(p.Key),
                                            Unescaped(":"),
                                            p.Value))));
        }
    }
}