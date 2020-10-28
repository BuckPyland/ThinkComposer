// -------------------------------------------------------------------------------------------
// Instrumind ThinkComposer
//
// Copyright (C) 2011-2015 Néstor Marcel Sánchez Ahumada.
// https://github.com/nmarcel/ThinkComposer
//
// This file is part of ThinkComposer, which is free software licensed under the GNU General Public License.
// It is provided without any warranty. You should find a copy of the license in the root directory of this software product.
// -------------------------------------------------------------------------------------------
//
// Project: Instrumind ThinkComposer v1.0
// File   : DialogOptionsWindow.cs
// Object : Instrumind.Common.Visualization.DialogOptionsWindow (Class)
//
// Date       Author             Changes
// ---------- ------------------ -------------------------------------------------------------
// 2009.06.19 Néstor Sánchez A.  Creation
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

/// Library of standard Instrumind WPF custom and user controls.
namespace Instrumind.Common.Visualization.Widgets
{
   /// <summary>
   /// Interaction logic for DialogOptionsWindow.xaml
   /// </summary>
   public partial class DialogOptionsWindow : Window
   {
      public static new readonly DependencyProperty TitleProperty;

      static DialogOptionsWindow()
      {
         TitleProperty = DependencyProperty.Register("Title",
            typeof(string),
            typeof(DialogOptionsWindow),
            new FrameworkPropertyMetadata("DialogOptionsWindow's Title",
            new PropertyChangedCallback(OnTitleChanged)));
      }

      public DialogOptionsWindow()
      {
         InitializeComponent();

         Owner = Application.Current.MainWindow;
         MessageText.Text = "";
         AdditionalInfoText.Text = "";
         OptionsPanel.Children.Clear();

         SizeChanged += AdjustSize;
         LocationChanged += AdjustSize;
      }

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="Title">Window title</param>
      /// <param name="Message">Dialog message</param>
      /// <param name="AdditionalInfo">Additional information</param>
      /// <param name="AdditionalPanel">Additional panel for extended options (such as checkboxes and radiobuttons)</param>
      /// <param name="SelectionAction">Action to be executed upon option-button selection (it will receive the selected option TechName)</param>
      /// <param name="DefaultOption">TechName for the option to be marked as default</param>
      /// <param name="Options">Collection of recognizable-elements to be shown as options (each must have a different Code)</param>
      public DialogOptionsWindow(string Title, string Message,
                                 string AdditionalInfo, Panel AdditionalPanel,
                                 Action<string> SelectionAction, string DefaultOption,
                                 params IRecognizableElement[] Options) : this()
      {
         // This is OK.
         // Must be established in the Loaded event when the template will be already applied.
         this.Title = "";
         //
         MessageText.Text = Message;
         AdditionalInfoText.Text = AdditionalInfo;

         SelectionButton FocusableButton = null;

         var RefWindow = this;

         Action<string> ExtendedSelectionAction = (techname) =>
         {
            SelectionAction(techname);
            RefWindow.Close();
         };

         if (Options != null && Options.Any())
         {
            if (DefaultOption.IsAbsent())
               DefaultOption = Options.First().TechName;

            foreach (var Option in Options)
            {
               var NewButton = new SelectionButton(Option, ExtendedSelectionAction);

               if (Option.TechName == DefaultOption)
               {
                  NewButton.IsDefault = true;
                  FocusableButton = NewButton;
               }

               OptionsPanel.Children.Add(NewButton);
            }

            // 2020-10-21 -- Buck added test for OptionsPanel.Children.Count > 0
            if (FocusableButton == null && OptionsPanel.Children.Count > 0)
               FocusableButton = OptionsPanel.Children[0] as SelectionButton;
         }

         if (AdditionalPanel != null)
            ContentPanel.Children.Add(AdditionalPanel);

         var TargetWindow = this;

         TargetWindow.Loaded += (sender, args) =>
         {
            var BtnRestoreOrMaximize = Display.GetTemplateChild<Button>(TargetWindow, "BtnRestoreOrMaximize", true);

            BtnRestoreOrMaximize.Visibility = Visibility.Collapsed;
            TargetWindow.Title = Title;

            if (FocusableButton != null)
               FocusableButton.Focus();
         };
      }

      private void AdjustSize(object sender, EventArgs args)
      {
         if (WindowState == System.Windows.WindowState.Maximized)
            return;

         //-? if (this.Left + this.ActualWidth > SystemParameters.WorkArea.Width)
         MaxWidth = (ExplicitMaxWidth.IsNan() ? SystemParameters.WorkArea.Width - Left
                                              : ExplicitMaxWidth).EnforceRange(SystemParameters.WorkArea.Width / 4.0, SystemParameters.WorkArea.Width);

         //-? if (this.Top + this.ActualHeight > SystemParameters.WorkArea.Height)
         MaxHeight = (ExplicitMaxHeight.IsNan() ? SystemParameters.WorkArea.Height - Top
                                                : ExplicitMaxHeight).EnforceRange(SystemParameters.WorkArea.Height / 4.0, SystemParameters.WorkArea.Height);
      }

      public double ExplicitMaxWidth = double.NaN;

      public double ExplicitMaxHeight = double.NaN;

      private void DialogOptionsWindow_MouseDown(object sender, MouseButtonEventArgs e)
      {
         DragMove();

         if (e.ClickCount == 2 && ResizeMode != ResizeMode.NoResize)
            BtnRestoreOrMaximize_Click(this, null);
      }

      private Thickness AlternateBorderPadding = new Thickness(8);

      private void BtnRestoreOrMaximize_Click(object sender, RoutedEventArgs e)
      {
         if (e != null)
            e.Handled = true;

         var BackPanel = Display.GetTemplateChild<Border>(this, "BackPanel");
         if (BackPanel == null)
            return;

         var SwapBorderPadding = BackPanel.Padding;
         BackPanel.Padding = AlternateBorderPadding;
         AlternateBorderPadding = SwapBorderPadding;

         if (WindowState == WindowState.Normal)
         {
            SizeToContent = SizeToContent.Manual;

            MaxWidth = SystemParameters.WorkArea.Width;     //- double.PositiveInfinity;
            MaxHeight = SystemParameters.WorkArea.Height;   //- double.PositiveInfinity;

            // Width = double.NaN;
            // Height = double.NaN;

            WindowState = WindowState.Maximized;
         }
         else
         {
            SizeToContent = SizeToContent.WidthAndHeight;

            WindowState = WindowState.Normal;
         }
      }

      private void BtnClose_Click(object sender, RoutedEventArgs e)
      {
         if (e != null)
            e.Handled = true;

         Close();
      }

      public new string Title
      {
         get { return (string)GetValue(DialogOptionsWindow.TitleProperty); }
         set { SetValue(DialogOptionsWindow.TitleProperty, value); }
      }

      private static void OnTitleChanged(DependencyObject depobj, DependencyPropertyChangedEventArgs evargs)
      {
         var textblock = Display.GetTemplateChild<TextBlock>(depobj, "TitleTextBlock");
         if (textblock == null)
            return;

         textblock.Text = evargs.NewValue as string;
      }

      private void Window_KeyDown(object sender, KeyEventArgs e)
      {
         // Needed because the Button.IsCancel property didn't work
         // (maybe for coming from a template)
         if (e.Key != Key.Escape)
            return;

         BtnClose_Click(this, e);

         //T Console.WriteLine(e.Key.ToString());
      }
   }
}
