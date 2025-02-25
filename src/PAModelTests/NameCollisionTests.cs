// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PAModelTests
{
    [TestClass]
    public class NameCollisionTests
    {

        [TestMethod]
        public void TestAssetFileRename()
        {
            var doc = new CanvasDocument();
            var resource1 = new ResourceJson()
            {
                Name = "Image", // Capital
                Path = "Assets\\Images\\Image.png",
                FileName = "Image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "Image.png"), new FileEntry());

            var resource2 = new ResourceJson()
            {
                Name = "image", // Lowercase
                Path = "Assets\\Images\\image.png",
                FileName = "image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "image.png"), new FileEntry());

            var resource3 = new ResourceJson()
            {
                Name = "image_1",
                Path = "Assets\\Images\\image_1.png",
                FileName = "image_1.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "image_1.png"), new FileEntry());

            doc._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { resource1, resource2, resource3 } };

            var errorContainer = new ErrorContainer();
            doc.StabilizeAssetFilePaths(errorContainer);

            var newFileNames = doc._resourcesJson.Resources.Select(resource => resource.Name);
            Assert.IsTrue(newFileNames.Contains("Image"));
            Assert.IsTrue(newFileNames.Contains("image_1"));
            Assert.IsTrue(newFileNames.Contains("image_2"));
        }

        [DataTestMethod]
        [DataRow("AppWithLabel.msapp")]
        [DataRow("DuplicateScreen.msapp")]
        public void TestScreenRename(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Create a list of screens expected to be seen in the output
                List<string> expectedScreens = msapp._screens.Keys.ToList();

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Look for the expected screens in the YAML files
                string srcPath = Path.Combine(outSrcDir, "Src");
                foreach (string yamlFile in Directory.GetFiles(srcPath, "*.fx.yaml", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(yamlFile).Replace(".fx.yaml", string.Empty);

                    // Check for an exact match between the screen name and the file.
                    if (expectedScreens.Contains(fileName))
                    {
                        expectedScreens.Remove(fileName);
                        continue;
                    }

                    // Replace any appended suffixes on Windows to see if there was a file collision.
                    fileName = Regex.Replace(fileName, "(?:_\\d+)?$", string.Empty);

                    // Check if the new file name without a suffix matches, otherwise fail the test
                    if (expectedScreens.Contains(fileName))
                    {
                        expectedScreens.Remove(fileName);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected file {yamlFile} in Src folder.");
                    }
                }

                // There should be no expected files that were not found
                Assert.AreEqual<int>(expectedScreens.Count, 0, $"{expectedScreens.Count} screens not found in Src directory.");
            }
        }

        [DataTestMethod]
        [DataRow("AppWithLabel.msapp")]
        [DataRow("DuplicateScreen.msapp")]
        [DataRow("ComponentNameCollision.msapp")]
        public void TestEditorStateRename(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Create a list of expected controles with an EditorState file
                List<string> expectedControlsWithEditorState = new List<string>();
                expectedControlsWithEditorState.AddRange(msapp._screens.Keys);
                expectedControlsWithEditorState.AddRange(msapp._components.Keys);

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Look for the expected controls in the EditorState files
                string srcPath = Path.Combine(outSrcDir, "Src", "EditorState");
                foreach (string editorStateFile in Directory.GetFiles(srcPath, "*.editorstate.json", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(editorStateFile).Replace(".editorstate.json", string.Empty);

                    // Check for an exact match between the control and the file.
                    if (expectedControlsWithEditorState.Contains(fileName))
                    {
                        expectedControlsWithEditorState.Remove(fileName);
                        continue;
                    }

                    // Replace any appended suffixes on Windows to see if there was a file collision.
                    fileName = Regex.Replace(fileName, "(?:_\\d+)?$", string.Empty);

                    // Check if the new file name without a suffix matches, otherwise fail the test
                    if (expectedControlsWithEditorState.Contains(fileName))
                    {
                        expectedControlsWithEditorState.Remove(fileName);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected file {editorStateFile} in EditorState folder.");
                    }
                }

                // There should be no expected files that were not found
                Assert.AreEqual<int>(expectedControlsWithEditorState.Count, 0, $"{expectedControlsWithEditorState.Count} editor state files not found in EditorState directory.");
            }
        }
    }
}
