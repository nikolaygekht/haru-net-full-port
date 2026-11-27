using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfJavaScriptTests
    {
        private static HpdfArray ScriptTree(HpdfDocument doc) =>
            (HpdfArray)((HpdfDict)((HpdfDict)doc.Catalog.Dict["Names"])["JavaScript"])["Names"];

        private static byte[] ScriptBytes(HpdfObject action) =>
            ((HpdfStreamObject)((HpdfDict)action)["JS"]).Stream.ToArray();

        [Fact]
        public void AddJavaScript_WritesJavaScriptActionInTheNameTree()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            doc.AddJavaScript("init", "var greeting = 'Hello';");
            doc.SaveToMemory();

            var names = ScriptTree(doc);
            names.Count.Should().Be(2);
            ((HpdfString)names[0]).GetText().Should().Be("init");
            var action = (HpdfDict)names[1];
            ((HpdfName)action["Type"]).Value.Should().Be("Action");
            ((HpdfName)action["S"]).Value.Should().Be("JavaScript");
            var stream = (HpdfStreamObject)action["JS"];
            stream.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
            stream.IsIndirect.Should().BeTrue();
            System.Text.Encoding.ASCII.GetString(ScriptBytes(action)).Should().Be("var greeting = 'Hello';");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void AddJavaScript_NonAsciiScript_IsUtf16TextStream()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            doc.AddJavaScript("init", "app.alert('Привет');");
            doc.SaveToMemory();

            var bytes = ScriptBytes(ScriptTree(doc)[1]);
            bytes.Take(2).Should().Equal(0xFE, 0xFF);
            System.Text.Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2).Should().Be("app.alert('Привет');");
        }

        [Fact]
        public void Save_SortsScriptsByName()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.AddJavaScript("b", "var b;");
            doc.AddJavaScript("a", "var a;");

            doc.SaveToMemory();

            var names = ScriptTree(doc);
            ((HpdfString)names[0]).GetText().Should().Be("a");
            ((HpdfString)names[2]).GetText().Should().Be("b");
        }

        [Fact]
        public void AddJavaScript_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            doc.AddJavaScript("init", "var x;");

            doc.Invoking(d => d.AddJavaScript("init", "var y;")).Should().Throw<HpdfException>("the name is taken");
            doc.Invoking(d => d.AddJavaScript("", "var y;")).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AddJavaScript(null!, "var y;")).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AddJavaScript("other", null!)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void PdfA_EnabledAfterTheScript_DoesNotAllowJavaScript()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.AddJavaScript("init", "var x;");
            doc.SetPdfACompliance();

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>()
                .Which.Message.Should().Contain("JavaScript");
        }

        [Fact]
        public void Save_WithAllNameTrees_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var destination = page.CreateDestination();
            destination.SetFit();
            doc.CreateNamedDestination("intro", destination);
            doc.AttachFile("a.txt", new byte[] { 1 });
            doc.AddJavaScript("init", "var x;");

            var first = doc.SaveToMemory();
            var second = doc.SaveToMemory();

            second.Should().Equal(first);
            ((HpdfDict)doc.Catalog.Dict["Names"]).Keys.Should().BeEquivalentTo("EmbeddedFiles", "Dests", "JavaScript");
        }
    }
}
