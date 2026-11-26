using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Forms;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test
{
    /// <summary>
    /// Tests for the generated appearances of text and choice fields and the form's default resources.
    /// </summary>
    public class HpdfFormVariableTextTests
    {
        private static readonly HpdfRect FieldRect = new HpdfRect(100, 600, 300, 620);

        // ---------- default resources ----------

        [Fact]
        public void Save_FormWithoutTextFields_HasHelveticaInDefaultResources()
        {
            var (doc, form, page) = NewForm();
            var checkbox = new HpdfCheckbox(doc.Xref, "Agree");
            AddField(form, page, checkbox, new HpdfRect(100, 100, 120, 120));

            doc.SaveToMemory();

            var font = DefaultResourceFont(form, "Helv");
            Name(font["BaseFont"]).Should().Be("Helvetica");
            Name(font["Subtype"]).Should().Be("Type1");
            Name(DefaultResourceFont(form, "ZaDb")["BaseFont"]).Should().Be("ZapfDingbats", "button captions use it");
        }

        [Fact]
        public void Save_FormWithoutButtons_HasNoZapfDingbats()
        {
            var (doc, form, page) = NewForm();
            AddField(form, page, new HpdfTextField(doc.Xref, "Name"), FieldRect);

            doc.SaveToMemory();

            ((HpdfDict)((HpdfDict)form.Dict["DR"])["Font"]).ContainsKey("ZaDb").Should().BeFalse();
        }

        [Fact]
        public void Save_FieldWithOwnFont_AddsItToDefaultResources()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Code");
            field.SetDefaultAppearance("/Cour 10 Tf 1 0 0 rg");
            field.SetValue("X-42");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            Name(DefaultResourceFont(form, "Cour")["BaseFont"]).Should().Be("Courier");
            Name(DefaultResourceFont(form, "Helv")["BaseFont"]).Should().Be("Helvetica");
            string content = AppearanceContent(widget);
            content.Should().Contain("/Cour 10 Tf 1 0 0 rg").And.Contain("(X-42) Tj");
            var resources = (HpdfDict)((HpdfDict)Normal(widget)["Resources"])["Font"];
            resources["Cour"].Should().BeSameAs(DefaultResourceFont(form, "Cour"));
        }

        // ---------- text fields ----------

        [Fact]
        public void Save_TextField_AppearanceShowsValueInDefaultAppearance()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            var normal = Normal(widget);
            Name(normal["Subtype"]).Should().Be("Form");
            ((HpdfArray)normal["BBox"]).Cast<HpdfReal>().Select(v => v.Value).Should().Equal(0f, 0f, 200f, 20f);
            string content = AppearanceContent(widget);
            content.Should().StartWith("/Tx BMC\n").And.EndWith("EMC\n");
            content.Should().Contain("1 1 198 18 re\nW\nn\n", "the text is clipped to the field");
            content.Should().Contain("/Helv 12 Tf 0 0 0 rg").And.Contain("(Hello) Tj");
            widget.Dict.ContainsKey("MK").Should().BeFalse("no frame was requested");
        }

        [Fact]
        public void Save_TextField_IsCenteredVertically()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            var font = doc.GetFont("Helvetica", null);
            float ascent = font.GetAscent() / 1000f * 12;
            float descent = font.GetDescent() / 1000f * 12;
            var (x, y) = TextPositions(AppearanceContent(widget)).Single();
            x.Should().BeApproximately(2, 0.01f);
            y.Should().BeApproximately((20 - (ascent - descent)) / 2 - descent, 0.01f);
        }

        [Theory]
        [InlineData(HpdfFieldAlignment.Left)]
        [InlineData(HpdfFieldAlignment.Center)]
        [InlineData(HpdfFieldAlignment.Right)]
        public void Save_TextField_AlignsTextByQuadding(HpdfFieldAlignment alignment)
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetAlignment(alignment);
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            float textWidth = doc.GetFont("Helvetica", null).MeasureText("Hello", 12);
            float expected = alignment switch
            {
                HpdfFieldAlignment.Center => (200 - textWidth) / 2,
                HpdfFieldAlignment.Right => 200 - 2 - textWidth,
                _ => 2
            };
            TextPositions(AppearanceContent(widget)).Single().X.Should().BeApproximately(expected, 0.01f);
        }

        [Fact]
        public void Save_TextFieldWithAutoSize_FitsTextIntoField()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetDefaultAppearance("/Helv 0 Tf 0 g");
            field.SetValue("A rather long value that does not fit at the full height");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            float size = FontSize(AppearanceContent(widget));
            size.Should().BeGreaterThan(0);
            doc.GetFont("Helvetica", null).MeasureText("A rather long value that does not fit at the full height", size)
                .Should().BeLessThanOrEqualTo(196.01f);
        }

        [Fact]
        public void Save_TextFieldWithAutoSize_ShortTextFillsHeight()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetDefaultAppearance("/Helv 0 Tf 0 g");
            field.SetValue("Hi");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            var bbox = doc.GetFont("Helvetica", null).GetBBox();
            FontSize(AppearanceContent(widget)).Should().BeApproximately(16 / ((bbox.Top - bbox.Bottom) / 1000f), 0.01f);
        }

        [Fact]
        public void Save_PasswordField_ShowsAsterisks()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Secret");
            field.SetPassword(true);
            field.SetValue("abc");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            AppearanceContent(widget).Should().Contain("(***) Tj").And.NotContain("abc");
        }

        [Fact]
        public void Save_MultilineField_WrapsTextFromTheTop()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Notes");
            field.SetMultiline(true);
            field.SetValue("one two three four five six seven eight nine ten\nlast");
            var widget = AddField(form, page, field, new HpdfRect(100, 500, 200, 600));

            doc.SaveToMemory();

            string content = AppearanceContent(widget);
            var lines = Regex.Matches(content, @"\((.*)\) Tj").Select(m => m.Groups[1].Value).ToArray();
            lines.Length.Should().BeGreaterThan(2);
            string.Join(" ", lines).Should().Be("one two three four five six seven eight nine ten last");
            lines.Last().Should().Be("last", "a line feed starts a new line");

            var positions = TextPositions(content);
            positions.Select(p => p.Y).Should().BeInDescendingOrder();
            var font = doc.GetFont("Helvetica", null);
            positions[0].Y.Should().BeApproximately(100 - 2 - font.GetAscent() / 1000f * 12, 0.01f);
            var widths = lines.Select(line => font.MeasureText(line, 12));
            widths.Should().OnlyContain(w => w <= 96.01f);
        }

        [Fact]
        public void Save_EmptyTextField_HasEmptyMarkedContent()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            AppearanceContent(widget).Should().Be("/Tx BMC\nEMC\n");
        }

        [Fact]
        public void Save_FieldWithFrame_DrawsFrameBehindText()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);
            widget.SetAppearanceCharacteristics(borderColor: new[] { 0f, 0f, 1f }, backgroundColor: new[] { 1f, 1f, 0.8f });

            doc.SaveToMemory();

            string content = AppearanceContent(widget);
            content.IndexOf("0 0 1 RG", System.StringComparison.Ordinal).Should().BeLessThan(content.IndexOf("/Tx BMC", System.StringComparison.Ordinal));
            content.Should().Contain("1 1 0.8 rg").And.Contain("(Hello) Tj");
        }

        [Fact]
        public void Save_RotatedWidget_RotatesAppearance()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);
            widget.SetAppearanceCharacteristics(rotation: 90);

            doc.SaveToMemory();

            var normal = Normal(widget);
            ((HpdfArray)normal["BBox"]).Cast<HpdfReal>().Select(v => v.Value).Should().Equal(0f, 0f, 20f, 200f);
            ((HpdfArray)normal["Matrix"]).Cast<HpdfNumber>().Select(v => v.Value).Should().Equal(0, 1, -1, 0, 0, 0);
        }

        [Fact]
        public void Save_Twice_ReusesAppearanceStreamAndShowsNewValue()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetValue("First");
            var widget = AddField(form, page, field, FieldRect);
            doc.SaveToMemory();
            var stream = Normal(widget);
            int objectCount = doc.Xref.Entries.Count;

            field.SetValue("Second");
            doc.SaveToMemory();

            Normal(widget).Should().BeSameAs(stream);
            doc.Xref.Entries.Count.Should().Be(objectCount);
            AppearanceContent(widget).Should().Contain("(Second) Tj").And.NotContain("First");
        }

        [Fact]
        public void Save_DefaultAppearanceWithUnknownFont_LeavesWidgetWithoutAppearance()
        {
            var (doc, form, page) = NewForm();
            var field = new HpdfTextField(doc.Xref, "Name");
            field.SetDefaultAppearance("/F1 10 Tf 0 g");
            field.SetValue("Hello");
            var widget = AddField(form, page, field, FieldRect);

            doc.SaveToMemory();

            widget.Dict.ContainsKey("AP").Should().BeFalse();
        }

        // ---------- choice fields ----------

        [Fact]
        public void Save_ComboBox_ShowsSelectedValue()
        {
            var (doc, form, page) = NewForm();
            var combo = new HpdfChoice(doc.Xref, "Country");
            combo.AddOptions("Canada", "Mexico", "USA");
            combo.SetValue("Mexico");
            var widget = AddField(form, page, combo, FieldRect);

            doc.SaveToMemory();

            string content = AppearanceContent(widget);
            content.Should().Contain("(Mexico) Tj").And.NotContain("Canada");
        }

        [Fact]
        public void Save_ListBox_ShowsOptionsAndHighlightsSelection()
        {
            var (doc, form, page) = NewForm();
            var list = new HpdfChoice(doc.Xref, "Colors", isComboBox: false);
            list.SetMultiSelect(true);
            list.AddOptions("Red", "Green", "Blue");
            list.SetSelectedValues("Red", "Blue");
            var widget = AddField(form, page, list, new HpdfRect(100, 500, 200, 600));

            doc.SaveToMemory();

            string content = AppearanceContent(widget);
            content.Should().Contain("(Red) Tj").And.Contain("(Green) Tj").And.Contain("(Blue) Tj");
            Regex.Matches(content, @"0\.6 0\.757 0\.855 rg").Count.Should().Be(2);

            var positions = TextPositions(content);
            positions.Select(p => p.X).Should().OnlyContain(x => x == 2);
            positions.Select(p => p.Y).Should().BeInDescendingOrder();
        }

        [Fact]
        public void Save_ListBoxWithoutSelection_HasNoHighlight()
        {
            var (doc, form, page) = NewForm();
            var list = new HpdfChoice(doc.Xref, "Colors", isComboBox: false);
            list.AddOptions("Red", "Green");
            var widget = AddField(form, page, list, new HpdfRect(100, 500, 200, 600));

            doc.SaveToMemory();

            AppearanceContent(widget).Should().Contain("(Red) Tj").And.Contain("(Green) Tj");
            Regex.Matches(AppearanceContent(widget), @"0\.6 0\.757 0\.855 rg").Count.Should().Be(0);
        }

        // ---------- reading the document back ----------

        [Fact]
        public void Save_FormWithAppearances_IsReadableByPdfPig()
        {
            var (doc, form, page) = NewForm();
            var name = new HpdfTextField(doc.Xref, "Name");
            name.SetValue("Jane");
            AddField(form, page, name, FieldRect);
            var combo = new HpdfChoice(doc.Xref, "Country");
            combo.AddOptions("Canada", "USA");
            combo.SetValue("USA");
            AddField(form, page, combo, new HpdfRect(100, 550, 300, 570));

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));

            pdf.TryGetForm(out var acroForm).Should().BeTrue();
            acroForm!.Fields.Count.Should().Be(2);
            pdf.GetPage(1).ExperimentalAccess.GetAnnotations().Count(a => a.HasNormalAppearance).Should().Be(2);
        }

        // ---------- helpers ----------

        private static (HpdfDocument Doc, HpdfAcroForm Form, HpdfPage Page) NewForm()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            return (doc, doc.GetOrCreateAcroForm(), page);
        }

        private static HpdfWidgetAnnotation AddField(HpdfAcroForm form, HpdfPage page, HpdfField field, HpdfRect rect)
        {
            var widget = field.CreateWidget(rect);
            page.AddWidgetAnnotation(widget);
            form.AddField(field);
            return widget;
        }

        private static HpdfDict DefaultResourceFont(HpdfAcroForm form, string name) =>
            (HpdfDict)((HpdfDict)((HpdfDict)form.Dict["DR"])["Font"])[name];

        private static HpdfStreamObject Normal(HpdfWidgetAnnotation widget) =>
            (HpdfStreamObject)((HpdfDict)widget.Dict["AP"])["N"];

        private static string AppearanceContent(HpdfWidgetAnnotation widget) =>
            System.Text.Encoding.ASCII.GetString(Normal(widget).Stream.ToArray());

        private static string Name(HpdfObject value) => ((HpdfName)value).Value;

        private static (float X, float Y)[] TextPositions(string content) =>
            Regex.Matches(content, @"1 0 0 1 (\S+) (\S+) Tm")
                .Select(m => (Number(m.Groups[1].Value), Number(m.Groups[2].Value)))
                .ToArray();

        private static float FontSize(string content) =>
            Number(Regex.Match(content, @"/Helv (\S+) Tf").Groups[1].Value);

        private static float Number(string text) => float.Parse(text, CultureInfo.InvariantCulture);
    }
}
