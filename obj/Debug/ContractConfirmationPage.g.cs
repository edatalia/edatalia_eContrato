﻿

#pragma checksum "C:\Users\agomez.ETIC\Documents\Visual Studio 2013\Projects\Edatalia_signplyRT\Edatalia_signplyRT\ContractConfirmationPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "8F2E2351A18817EADC157BDD4EA2F82B"
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
    partial class ContractConfirmationPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 82 "..\..\ContractConfirmationPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Checked += this.chbConfidentialTems_Checked;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 96 "..\..\ContractConfirmationPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AppBarButtonGenerateContract_Click;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}

