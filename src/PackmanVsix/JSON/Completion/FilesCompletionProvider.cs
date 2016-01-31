﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    class FilesCompletionProvider : BaseCompletionProvider
    {

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.ArrayElement; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem.FindType<JSONMember>();

            if (member == null || member.UnquotedNameText != "files")
                yield break;

            var parent = member.Parent as JSONObject;
            var name = parent?.FindType<JSONMember>()?.UnquotedNameText;

            if (string.IsNullOrEmpty(name))
                yield break;

            var children = parent.BlockItemChildren?.OfType<JSONMember>();
            var version = children?.SingleOrDefault(c => c.UnquotedNameText == "version");

            if (version == null)
                yield break;

            var package = VSPackage.Manager.Provider.GetInstallablePackageAsync(name, version.UnquotedValueText).Result;

            if (package == null)
                yield break;

            Telemetry.TrackEvent("Completion for files");

            JSONArray array = context.ContextItem.FindType<JSONArray>();

            if (array == null)

                yield break;

            HashSet<string> usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (JSONArrayElement arrayElement in array.Elements)
            {
                JSONTokenItem token = arrayElement.Value as JSONTokenItem;

                if (token != null)
                {
                    usedFiles.Add(token.CanonicalizedText);
                }
            }

            FrameworkElement o = context.Session.Presenter as FrameworkElement;

            if (o != null)
            {
                o.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, new Binding("Background")
                {
                    Source = o,
                    Converter = new BrushToColorConverter()
                });
            }

            foreach (var file in package.AllFiles)
            {
                if (!usedFiles.Contains(file))
                {
                    bool isThemeIcon;
                    ImageSource glyph = WpfUtil.GetIconForFile(o, file, out isThemeIcon);

                    yield return new SimpleCompletionEntry(file, glyph, context.Session);
                }
            }

            if (o != null)
            {
                BindingOperations.ClearBinding(o, ImageThemingUtilities.ImageBackgroundColorProperty);
            }
            //}
        }
    }
}