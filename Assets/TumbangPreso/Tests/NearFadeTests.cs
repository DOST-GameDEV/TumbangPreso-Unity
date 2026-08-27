using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Visual;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The near-camera dissolve, asserted from the source rather than from a picture.
    ///
    /// ⚠️⚠️ IT READS THE SHADER AND THE SCENES AS TEXT, THE SAME REASON `MapGradeSanityTests` AND
    /// `SceneScriptCheck` DO. Everything this feature can get wrong is invisible to a render taken
    /// from a normal camera position: the band only does anything inside 1.8 m of a prop, the
    /// prefix list only matters if a map renames a pole, and the shader is only in the player at
    /// all if `GameBuilder` puts it in the always-included list. A showcase render points at the
    /// street from across it and would pass with every one of those broken.
    ///
    /// ⚠️ AND IT IS THE GUARD ON A NUMBER LIVING IN TWO PLACES. `NearFade` writes the band onto
    /// the material and `NearFade.shader` also declares defaults for it, which is the arrangement
    /// `Design.md` opens by calling a bug in one of the two halves. The test is what makes it
    /// merely an arrangement.
    /// </summary>
    public sealed class NearFadeTests
    {
        private const string ShaderPath = "Assets/TumbangPreso/Shaders/NearFade.shader";
        private const string InstallerPath = "Assets/TumbangPreso/Runtime/Visual/NearFade.cs";
        private const string ColourPassPath = "Assets/TumbangPreso/Runtime/Visual/EnvColourPass.cs";
        private const string BuilderPath = "Assets/TumbangPreso/Editor/GameBuilder.cs";
        private const string CameraRigPath = "Assets/TumbangPreso/Runtime/Camera/CameraRig.cs";
        private const string MapDirectory = "Assets/TumbangPreso/Scenes/Maps";

        private static string Read(string path)
        {
            Assert.IsTrue(File.Exists(path), $"{path} is missing.");
            return File.ReadAllText(path);
        }

        [Test]
        public void TheShaderDeclaresTheNameTheInstallerLooksFor()
        {
            // ⚠️ A `Shader.Find` MISS IS SILENT BY DESIGN HERE: `NearFade.FindShader` warns and
            // leaves every prop solid, which is the original bug back with a log line beside it.
            // Renaming one half of this pair and not the other is the cheapest way to cause that.
            StringAssert.Contains($"Shader \"{NearFade.ShaderName}\"", Read(ShaderPath));
        }

        [Test]
        public void TheShaderIsAlwaysIncludedSoItSurvivesAPlayerBuild()
        {
            // ⚠️ NOTHING IN ANY SCENE REFERENCES THIS SHADER, so the build strips it unless
            // `GameBuilder.EnsureRuntimeShaders` names it. That failure works perfectly in the
            // editor and ships the reported bug in the .exe, which is the split this list exists
            // to close. Read as text because the Editor assembly is not referenced from here.
            StringAssert.Contains($"\"{NearFade.ShaderName}\"", Read(BuilderPath));
        }

        [Test]
        public void TheInstallerIsActuallyReached()
        {
            // ⚠️ THE WHOLE FEATURE HANGS OFF ONE CALL IN `EnvColourPass.Apply`, and this project's
            // signature failure is a thing that is built, tested and never called. `DeadFeatureAudit`
            // exists for that shape; this is the same guard for this feature.
            StringAssert.Contains("NearFade.Install(", Read(ColourPassPath));
        }

        [Test]
        public void TheBandIsOrderedAndClearsTheNearPlaneComfortably()
        {
            Assert.Less(NearFade.FadeEndMetres, NearFade.FadeStartMetres,
                        "The fade must complete NEARER than it starts; `smoothstep` is undefined " +
                        "when its edges are the wrong way round.");

            // The near plane is read out of `CameraRig` rather than repeated, because repeating it
            // is how the two would come to disagree.
            var match = Regex.Match(Read(CameraRigPath), @"nearClipPlane\s*=\s*([0-9.]+)f");
            Assert.IsTrue(match.Success, "CameraRig no longer sets nearClipPlane in a form this " +
                                         "test can read.");

            float nearPlane = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

            // ⚠️ FIVE TIMES, NOT MERELY GREATER. The point of the inner edge is that the prop is
            // completely gone before the near plane can slice a solid cross-section through it and
            // show the inside of the mesh. Equal-to would satisfy "greater" and fail the game.
            Assert.Greater(NearFade.FadeEndMetres, nearPlane * 5.0f,
                           $"The fade must finish well clear of the {nearPlane} m near plane.");
        }

        [Test]
        public void TheShaderDefaultsAgreeWithTheInstaller()
        {
            string shader = Read(ShaderPath);

            AssertDefault(shader, "_NearFadeStart", NearFade.FadeStartMetres);
            AssertDefault(shader, "_NearFadeEnd", NearFade.FadeEndMetres);
            AssertDefault(shader, "_NearFadeCell", NearFade.DitherCellPixels);
        }

        private static void AssertDefault(string shader, string property, float expected)
        {
            // ⚠️ THE `.*` IS GREEDY ON PURPOSE. A property line is
            // `_NearFadeCell ("Dither Cell, px", Range(1, 8)) = 2`, so a lazy or negated-class
            // match stops at the `)` that closes `Range(` and never reaches the default. Greedy
            // backtracks to the LAST `)` that is followed by `= <number>`, which is the right one.
            // `.` does not cross lines here, so this cannot run away into the next property.
            var match = Regex.Match(shader, Regex.Escape(property) + @"\s*\(.*\)\s*=\s*([0-9.]+)");

            Assert.IsTrue(match.Success, $"{property} has no readable default in the shader.");

            float declared = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

            Assert.AreEqual(expected, declared, 0.0001f,
                            $"{property} disagrees between NearFade.cs and NearFade.shader. One " +
                            "of the two is a bug.");
        }

        [Test]
        public void TheDitherIsIndexedInScreenSpace()
        {
            string shader = Read(ShaderPath);

            // ⚠️⚠️ THIS IS THE ONE PROPERTY THE EFFECT CANNOT SURVIVE LOSING. Indexed by UV or by
            // object space the pattern is painted ON the surface, so it slides and rotates with
            // the prop as the player walks and reads as crawling noise rather than as a screen
            // door. A future "simplification" to `IN.uv_MainTex` would compile, would still
            // dissolve, and would look wrong in a way that is hard to name from a still.
            StringAssert.Contains("float4 screenPos;", shader);
            StringAssert.Contains("_ScreenParams", shader);
            StringAssert.Contains("clip(", shader);
        }

        [Test]
        public void TheShadowCasterIsNotDithered()
        {
            // ⚠️ `addshadow` WOULD RUN `surf` IN THE SHADOW PASS, where `_WorldSpaceCameraPos` is
            // the LIGHT rather than the player, so the threshold would be measured from a position
            // that has nothing to do with where anybody is standing. Even done correctly it is
            // wrong to want: a shadow that dissolves as you walk up to the thing casting it reads
            // as a rendering fault. The `Fallback` supplies a solid caster instead.
            //
            // ⚠️ IT TESTS THE `#pragma` LINE, NOT THE FILE. The word `addshadow` appears several
            // times in the comment that explains why it is absent, so a whole-file search would
            // fail against a correct shader and would be "fixed" by deleting the explanation.
            var pragma = Regex.Match(Read(ShaderPath), @"#pragma\s+surface[^\r\n]*");

            Assert.IsTrue(pragma.Success, "NearFade.shader has no #pragma surface line.");
            StringAssert.DoesNotContain("addshadow", pragma.Value);
        }

        [Test]
        public void TheRenderTypeStaysOpaqueSoTheDepthNormalsPrepassIsCorrect()
        {
            // ⚠️⚠️ SEE § THE OUTLINE IN THE SHADER. `WorldOutline` reads
            // `_CameraDepthNormalsTexture`, which the built-in pipeline fills through a REPLACEMENT
            // shader chosen by this tag. "Opaque" keeps the prop's ink correct at every distance
            // and keeps it occluding the edges of what stands behind it; any other value drops it
            // out of that texture entirely and pays for it on every frame instead of inside the
            // 1.8 m band. Changing this is a look decision, not a cleanup, so it fails here first.
            StringAssert.Contains("\"RenderType\" = \"Opaque\"", Read(ShaderPath));
        }

        /// <summary>
        /// ⚠️⚠️ THE PREFIX LIST IS THE WHOLE SCOPE OF THE FEATURE, AND A RENAMED PROP WOULD TURN IT
        /// OFF IN COMPLETE SILENCE. `NearFade.Install` logs the count it matched, but nothing reads
        /// a log, and a map whose poles were renamed would simply go back to filling half the
        /// screen with no error anywhere. These are the counts measured off the shipped scenes:
        /// twelve `Poste_*` on Eskinita and twenty-eight `SidewalkPole_*` on Ilalim ng Tulay.
        ///
        /// ⚠️ ASSERTED AS "AT LEAST", NOT EXACTLY. Adding poles to a map is dressing work and must
        /// not fail a test; removing the last one, or renaming the family, is what this catches.
        /// </summary>
        [Test]
        public void EveryOccluderPrefixStillMatchesSomethingInAShippedScene()
        {
            var scenes = new List<string>(Directory.GetFiles(MapDirectory, "*.unity"));
            Assert.IsNotEmpty(scenes, $"No map scenes under {MapDirectory}.");

            var text = new Dictionary<string, string>();
            foreach (string scene in scenes) text[scene] = File.ReadAllText(scene);

            foreach (string prefix in NearFade.OccluderPrefixes)
            {
                int total = 0;

                foreach (var pair in text)
                    total += Regex.Matches(pair.Value, Regex.Escape(prefix) + @"_[A-Za-z0-9_]+").Count;

                Assert.Greater(total, 0,
                               $"NearFade.OccluderPrefixes carries '{prefix}' and no shipped map " +
                               "has an object by that name any more. Either the prop was renamed, " +
                               "in which case fix the prefix, or it is gone, in which case drop it.");
            }
        }

        [Test]
        public void BothMapsStillCarryThePolesTheReportWasAbout()
        {
            AssertDistinct("Eskinita.unity", @"Poste_[0-9]+", 12);
            AssertDistinct("IlalimNgTulay.unity", @"SidewalkPole_[EW]_[0-9]+", 28);
        }

        private static void AssertDistinct(string scene, string pattern, int atLeast)
        {
            string path = Path.Combine(MapDirectory, scene);
            var names = new HashSet<string>();

            foreach (Match m in Regex.Matches(Read(path), pattern)) names.Add(m.Value);

            Assert.GreaterOrEqual(names.Count, atLeast,
                                  $"{scene} carries {names.Count} objects matching {pattern}, " +
                                  $"and {atLeast} were measured off it when the near-camera " +
                                  "dissolve was written.");
        }

        [Test]
        public void TheInstallerHandlesEverySubmeshRatherThanOnlyTheFirst()
        {
            // ⚠️ `env_post_electric.mtl` DECLARES FOUR MATERIALS (timber, wire, drum, rust), so a
            // post is four submeshes and four material slots. `EnvColourPass.Paint` reads the
            // SINGULAR `sharedMaterial` and therefore only ever touches slot 0, which is survivable
            // for a tint and is not survivable here: three of the four slots would stay solid and
            // the post would dissolve into a wireframe of its own fittings.
            string installer = Read(InstallerPath);

            StringAssert.Contains("renderer.sharedMaterials", installer);
            StringAssert.DoesNotContain("renderer.sharedMaterial =", installer);
        }
    }
}
