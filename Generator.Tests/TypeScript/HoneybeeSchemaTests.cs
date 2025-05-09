using NSwag;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using SchemaGenerator;
using TemplateModels.TypeScript;

namespace Generator.Tests.TypeScript
{
    public class HoneybeeSchemaTests
    {

        static OpenApiDocument doc = null;
        [SetUp]
        public void Setup()
        {

            var jsonFile = Path.Combine(TestHelper.HoneybeeDir, "model_inheritance.json");

            var json = File.ReadAllText(jsonFile);
            doc = OpenApiDocument.FromJsonAsync(json).Result;

        }


        [Test]
        public void TestArrayOfAnyOfTypes()
        {
            var json = doc.Components.Schemas["Light"];
            var prop = json.ActualProperties.First(_ => _.Key == "dependencies").Value;

            Assert.That(prop.IsArray, Is.True);

            // test any type
            Assert.That(prop.Item.AnyOf.Count, Is.AtLeast(1));

            // test PropertyModel
            var pm = new PropertyTemplateModel("dependencies", prop);
            var tp = pm.Type;
            StringAssert.AreEqualIgnoringCase(tp, "(Plastic | Glass | BSDF | Glow | Light | Trans | Metal | Void | Mirror) []");

        }

        //GlobalModifierSet
        [Test]
        public void TestGlobalModifierSet()
        {
            var json = doc.Components.Schemas["GlobalModifierSet"];
            var prop = json.ActualProperties.First(_ => _.Key == "modifiers").Value;

            Assert.That(prop.IsArray, Is.True);

            // test any type
            Assert.That(prop.Item.AnyOf.Count, Is.AtLeast(1));

            // test PropertyModel
            var pm = new PropertyTemplateModel("modifiers", prop);
            var tp = pm.Type;
            StringAssert.AreEqualIgnoringCase(tp, "(Plastic | Glass | Trans) []");
            var df = pm.DefaultCodeFormat;
            StringAssert.Contains("[Plastic.fromJS(", df);

            Assert.That(pm.TransformDecorator, Is.Not.Null);

        }

        [Test]
        public void TestScheduleDay()
        {
            var json = doc.Components.Schemas["ScheduleDay"];
            var prop = json.ActualProperties.First(_ => _.Key == "times").Value;
            Assert.That(prop.IsArray, Is.True);

            // test PropertyModel
            var pm = new PropertyTemplateModel("times", prop);
            var tp = pm.Type;
            StringAssert.AreEqualIgnoringCase(tp, "number [] []");
            var df = pm.DefaultCodeFormat;
            StringAssert.Contains("[[0, 0]]", df);
        }

        [Test]
        public void TestRadianceShadeStateAbridged()
        {
            var json = doc.Components.Schemas["RadianceShadeStateAbridged"];

            var pm = new ClassTemplateModel(doc, json);
            Assert.That(pm.IsAbstract, Is.False);
        }

        [Test]
        public void TestPropertyDecoratorsScheduleDay()
        {
            var json = doc.Components.Schemas["ScheduleDay"];
            var classModel = new ClassTemplateModel(doc, json);
            Assert.That(classModel, Is.Not.Null);

            var weathers = classModel.Properties.FirstOrDefault(_ => _.PropertyName == "times");
            Assert.That(weathers, Is.Not.Null);
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsArray()");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsArray({ each: true })");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@ValidateNested({each: true })");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@Type(() => Array)");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsInt({ each: true })");

        }

        [Test]
        public void TestPropertyDecoratorsView()
        {
            var json = doc.Components.Schemas["View"];
            var classModel = new ClassTemplateModel(doc, json);
            Assert.That(classModel, Is.Not.Null);

            var weathers = classModel.Properties.FirstOrDefault(_ => _.PropertyName == "position");
            Assert.That(weathers, Is.Not.Null);
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsArray()");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsNumber({},{ each: true })");

        }


        [Test]
        public void TestNestedNumberArrayDecorators()
        {
            var json = doc.Components.Schemas["Face3D"];
            var classModel = new ClassTemplateModel(doc, json);
            Assert.That(classModel, Is.Not.Null);

            var weathers = classModel.Properties.FirstOrDefault(_ => _.PropertyName == "boundary");
            Assert.That(weathers, Is.Not.Null);
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsArray()");
            CollectionAssert.Contains(weathers.ValidationDecorators, "@IsNestedNumberArray()");

        }
    }
}