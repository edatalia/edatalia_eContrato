﻿

#pragma checksum "C:\Users\agomez.ETIC\Documents\Visual Studio 2013\Projects\Edatalia_signplyRT\Edatalia_signplyRT\InitPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "06B598E50FB1F481B27EBA5C6E7F5688"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Edatalia_signplyRT
{
    partial class InitPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 68 "..\..\..\InitPage.xaml"
                ((global::Windows.UI.Xaml.Controls.SearchBox)(target)).QuerySubmitted += this.searchBox_QuerySubmitted;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 89 "..\..\..\InitPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AppBarButtonSaveClient_Click;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 90 "..\..\..\InitPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AppBarButtonOpen_Click;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 91 "..\..\..\InitPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AppBarButtonConfirm_Click;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


