using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Forms;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test
{
    /// <summary>
    /// Tests for widget appearance states, generated appearance streams and appearance characteristics.
    /// </summary>
    public class HpdfFormAppearanceTests
    {
        private static readonly HpdfRect Box = new HpdfRect(100, 100, 120, 120);

        // ---------- appearance state follows the value ----------

        [Theory]
        [InlineData(true, "Yes")]
        [InlineData(false, "Off")]
        public void Checkbox_ValueSetBeforeWidgetIsCreated_WidgetGetsAppearanceState(bool isChecked, string expected)
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");
            checkbox.SetChecked(isChecked);

            var widget = checkbox.CreateWidget(Box);

            NameOf(widget.Dict["AS"]).Should().Be(expected);
        }

        [Fact]
        public void Checkbox_NeverSet_WidgetIsOff()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");

            var widget = checkbox.CreateWidget(Box);

            NameOf(widget.Dict["AS"]).Should().Be("Off");
        }

        [Fact]
        public void Checkbox_SetValueAfterWidgetIsCreated_UpdatesAppearanceState()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");
            var widget = checkbox.CreateWidget(Box);

            checkbox.SetValue("Yes");

            NameOf(widget.Dict["AS"]).Should().Be("Yes");
            NameOf(checkbox.Dict["V"]).Should().Be("Yes");
        }

        [Fact]
        public void Checkbox_WidgetCreatedThroughBaseClass_HasStateAppearances()
        {
            HpdfField checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");

            var widget = checkbox.CreateWidget(Box);

            var normal = (HpdfDict)((HpdfDict)widget.Dict["AP"])["N"];
            normal.Keys.Should().BeEquivalentTo("Yes", "Off");
        }

        [Fact]
        public void RadioButton_SetValue_UpdatesAppearanceStateOfEveryOption()
        {
            var radio = new HpdfRadioButton(new HpdfDocument().Xref, "Color");
            var red = radio.CreateOption("Red", Box);
            var blue = radio.CreateOption("Blue", Box);
            radio.SetSelected("Red");

            radio.SetValue("Blue");

            NameOf(red.Dict["AS"]).Should().Be("Off");
            NameOf(blue.Dict["AS"]).Should().Be("Blue");
        }

        // ---------- frames and appearance characteristics ----------

        [Fact]
        public void Checkbox_ByDefault_DrawsBlackBoxInBothStatesAndDescribesItInMk()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");

            var widget = checkbox.CreateWidget(Box);

            string off = AppearanceContent(widget, "Off");
            string on = AppearanceContent(widget, "Yes");
            off.Should().Contain("0 0 0 RG").And.Contain("0.5 0.5 19 19 re").And.Contain("S");
            on.Should().StartWith(off, "the on state is the frame plus the mark");
            on.Length.Should().BeGreaterThan(off.Length);

            var mk = (HpdfDict)widget.Dict["MK"];
            ((HpdfArray)mk["BC"]).Cast<HpdfReal>().Select(c => c.Value).Should().Equal(0f, 0f, 0f);
            Text(mk["CA"]).Should().Be("4");
        }

        [Fact]
        public void RadioButton_ByDefault_DrawsOutlineInOffStateAndUsesCircleCaption()
        {
            var radio = new HpdfRadioButton(new HpdfDocument().Xref, "Color");

            var widget = radio.CreateOption("Red", Box);

            AppearanceContent(widget, "Off").Should().Contain("0 0 0 RG").And.Contain(" c");
            Text(((HpdfDict)widget.Dict["MK"])["CA"]).Should().Be("l");
        }

        [Fact]
        public void SetAppearanceCharacteristics_AfterWidgetIsCreated_RedrawsAppearanceStreams()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");
            var widget = checkbox.CreateWidget(Box);

            widget.SetAppearanceCharacteristics(borderColor: new[] { 1f, 0f, 0f }, backgroundColor: new[] { 0f, 1f, 0f });

            string off = AppearanceContent(widget, "Off");
            off.Should().Contain("1 0 0 RG").And.Contain("0 1 0 rg").And.NotContain("0 0 0 RG");
            var mk = (HpdfDict)widget.Dict["MK"];
            mk.ContainsKey("BG").Should().BeTrue();
            Text(mk["CA"]).Should().Be("4", "the caption survives a change of colors");
        }

        [Fact]
        public void SetAppearanceCharacteristics_WithoutColors_RemovesFrame()
        {
            var checkbox = new HpdfCheckbox(new HpdfDocument().Xref, "Agree");
            var widget = checkbox.CreateWidget(Box);

            widget.SetAppearanceCharacteristics();

            AppearanceContent(widget, "Off").Should().BeEmpty();
            ((HpdfDict)widget.Dict["MK"]).ContainsKey("BC").Should().BeFalse();
        }

        [Fact]
        public void TextField_Widget_GetsNoGeneratedAppearance()
        {
            var field = new HpdfTextField(new HpdfDocument().Xref, "Name");

            var widget = field.CreateWidget(Box);

            widget.Dict.ContainsKey("AP").Should().BeFalse();
            widget.Dict.ContainsKey("MK").Should().BeFalse();
        }

        // ---------- signature ----------

        [Fact]
        public void Signature_Widget_HasFrameAppearance()
        {
            var signature = new HpdfSignature(new HpdfDocument().Xref, "Signature");

            var widget = signature.CreateWidget(new HpdfRect(10, 10, 210, 50));

            var normal = (HpdfStreamObject)((HpdfDict)widget.Dict["AP"])["N"];
            NameOf(normal["Subtype"]).Should().Be("Form");
            ((HpdfArray)normal["BBox"]).Cast<HpdfReal>().Select(c => c.Value).Should().Equal(0f, 0f, 200f, 40f);
            ContentOf(normal).Should().Contain("0.5 0.5 199 39 re");
            ((HpdfDict)widget.Dict["MK"]).ContainsKey("CA").Should().BeFalse();
            signature.Dict.ContainsKey("V").Should().BeFalse("the field stays unsigned");
        }

        [Fact]
        public void AcroForm_WithSignatureField_SetsSignaturesExistFlag()
        {
            var doc = new HpdfDocument();
            var form = doc.GetOrCreateAcroForm();

            form.AddField(new HpdfTextField(doc.Xref, "Name"));
            form.Dict.ContainsKey("SigFlags").Should().BeFalse();

            form.AddField(new HpdfSignature(doc.Xref, "Signature"));

            ((HpdfNumber)form.Dict["SigFlags"]).Value.Should().Be(1);
        }

        // ---------- helpers ----------

        private static string NameOf(HpdfObject value) => ((HpdfName)value).Value;

        private static string Text(HpdfObject value) => System.Text.Encoding.ASCII.GetString(((HpdfString)value).Value);

        private static string AppearanceContent(HpdfWidgetAnnotation widget, string state) =>
            ContentOf((HpdfStreamObject)((HpdfDict)((HpdfDict)widget.Dict["AP"])["N"])[state]);

        private static string ContentOf(HpdfStreamObject stream) =>
            System.Text.Encoding.ASCII.GetString(stream.Stream.ToArray());
    }
}
