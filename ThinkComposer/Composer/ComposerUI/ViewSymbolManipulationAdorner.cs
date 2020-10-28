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
// File   : ViewSymbolManipulationAdorner.cs
// Object : Instrumind.ThinkComposer.Composer.ComposerUI.ViewSymbolManipulationAdorner (Class)
//
// Date       Author             Changes
// ---------- ------------------ -------------------------------------------------------------
// 2009.10.05 Néstor Sánchez A.  Creation
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Instrumind.Common;
using Instrumind.Common.Visualization;

using Instrumind.ThinkComposer.ApplicationProduct;
using Instrumind.ThinkComposer.MetaModel;
using Instrumind.ThinkComposer.MetaModel.GraphMetaModel;
using Instrumind.ThinkComposer.Model.GraphModel;
using Instrumind.ThinkComposer.Model.VisualModel;

/// Provides the user-interface components for the Composition Composer.
namespace Instrumind.ThinkComposer.Composer.ComposerUI
{
   /// <summary>
   /// Presents visual cues for manipulating view symbol elements.
   /// </summary>
   public class ViewSymbolManipulationAdorner : ViewManipulationAdorner
   {
      static ImageSource ImgSrcSwitchContent = null;
      static ImageSource ImgSrcSwitchRelated = null;
      static ImageSource ImgSrcEditFormat = null;

      public const double ACTIONER_SIZE = 20;

      public const double EDITAREA_MIN_WIDTH = 32.0;
      public const double EDITAREA_MIN_HEIGHT = 16.0;

      public VisualSymbol ManipulatedSymbol
      {
         get { return ManipulatedObject as VisualSymbol; }
      }

      internal Rect ManipulatingHeadingRectangle;
      internal Rect ManipulatingHeadingLabel;
      internal Rect ManipulatingDetailsRectangle;
      internal bool IsManipulatingHeading = true;

      public ESymbolManipulationAction IntendedAction
      {
         get { return (ESymbolManipulationAction)IntendedAction_; }
         protected set { IntendedAction_ = (byte)value; }
      }
      public ESymbolManipulationAction TentativeAction
      {
         get { return (ESymbolManipulationAction)TentativeAction_; }
         protected set { TentativeAction_ = (byte)value; }
      }

      DrawingVisual FrmPointingHeadingPanel = null;
      DrawingVisual FrmPointingDetailsPanel = null;
      DrawingVisual FrmEditZone = null;
      Dictionary<Visual, MarkerAssignment> FrmMarkingZones = new Dictionary<Visual, MarkerAssignment>();
      Dictionary<Visual, DetailDesignator> FrmDetailTitleDesignateZones = new Dictionary<Visual, DetailDesignator>();
      Dictionary<Visual, DetailDesignator> FrmDetailTitleExpandZones = new Dictionary<Visual, DetailDesignator>();
      Dictionary<Visual, DetailDesignator> FrmDetailContentAssignZones = new Dictionary<Visual, DetailDesignator>();
      Dictionary<Visual, DetailDesignator> FrmDetailContentEditZones = new Dictionary<Visual, DetailDesignator>();

      Pen FrmPencil = new Pen(Brushes.LightCyan, 0);
      Brush FrmStroke = Brushes.Yellow;
      Brush FrmStrokeEdit = Brushes.Goldenrod;

      double FrmOpacity = 0.2;
      double FrmOpacityEdit = 0.2;

      Pen ActPencil = new Pen(Brushes.Gold, 1);
      Brush ActStroke = Brushes.PaleGoldenrod;

      Pen IndPencil = new Pen(Brushes.Blue, 1);
      Brush IndStroke = Brushes.White;

      DrawingVisual IndHeadingTop = null;
      DrawingVisual IndHeadingBottom = null;
      DrawingVisual IndHeadingLeft = null;
      DrawingVisual IndHeadingRight = null;

      DrawingVisual IndHeadingTopLeft = null;
      DrawingVisual IndHeadingTopRight = null;
      DrawingVisual IndHeadingBottomLeft = null;
      DrawingVisual IndHeadingBottomRight = null;

      DrawingVisual IndDetailsBottom = null;
      DrawingVisual IndDetailsLeft = null;
      DrawingVisual IndDetailsRight = null;
      DrawingVisual IndDetailsBottomLeft = null;
      DrawingVisual IndDetailsBottomRight = null;

      DrawingVisual ActShowComposite = null;
      DrawingVisual ActSwitchRelated = null;
      DrawingVisual ActSwitchDetails = null;
      DrawingVisual ActSwitchCompositeView = null;
      DrawingVisual ActAddDetail = null;

      public EManipulationDirection ResizingDirection { get; protected set; }

      /// <summary>
      /// Constructor.
      /// </summary>
      public ViewSymbolManipulationAdorner(ViewManipulationManager OwnerManager, VisualSymbol TargetSymbol, AdornerLayer TargetLayer,
                                           Action<ViewManipulationAdorner, bool, bool, bool, bool, bool, bool, bool, bool> ManipulationOperation)
           : base(OwnerManager, TargetSymbol, TargetLayer)
      {
         DefaultAction_ = (byte)ESymbolManipulationAction.EditInPlace;

         ManipulatingHeadingRectangle = TargetSymbol.BaseArea;
         ManipulatingHeadingLabel = TargetSymbol.LabelingArea;

         ManipulatingDetailsRectangle = TargetSymbol.DetailsArea;
         this.ManipulationOperation = ManipulationOperation;
      }

      // -----------------------------------------------------------------------------------------------------------------------
      public override void Visualize(bool Show = true, bool OnlyAdornAsSelected = false)
      {
         // Validate that the Adorner still points something.
         // Else, maybe an "Undo" was performed, so the Represented-Idea may not exist anymore.
         if (ManipulatedSymbol == null || ManipulatedSymbol.OwnerRepresentation == null)
         {
            if (ManipulatedSymbol != null)
               OwnerManager.RemoveAdorners();

            OwnerManager.OwnerView.UnselectAllObjects();

            return;
         }

         var WorkingHeadingRectangle = ManipulatingHeadingRectangle;

         if (ManipulatedSymbol.IsHidden)
            if (ManipulatedSymbol.CanShowNameOverCenter
                || ManipulatedSymbol.CanShowDefinitionOverTop)
               WorkingHeadingRectangle = ManipulatingHeadingLabel;
            else
               return;     // Because this is "hidden" (or shown too little to be noticed by user).

         // Reset previous drawn indicators
         ClearAllIndicators();

         if (!Show)
            return;

         // Show every other selected objects
         VisualizeOtherAffectedObjects();

         // Determine pointing areas
         var PointingHeadingArea = new Rect(WorkingHeadingRectangle.X - VisualSymbol.INDICATOR_SIZE / 2.0, WorkingHeadingRectangle.Y - VisualSymbol.INDICATOR_SIZE / 2.0,
                                            WorkingHeadingRectangle.Width + VisualSymbol.INDICATOR_SIZE, WorkingHeadingRectangle.Height + VisualSymbol.INDICATOR_SIZE);

         var PointingDetailsArea = new Rect(ManipulatingDetailsRectangle.X - VisualSymbol.INDICATOR_SIZE / 2.0, ManipulatingDetailsRectangle.Y - VisualSymbol.INDICATOR_SIZE / 2.0,
                                            ManipulatingDetailsRectangle.Width + VisualSymbol.INDICATOR_SIZE, ManipulatingDetailsRectangle.Height + VisualSymbol.INDICATOR_SIZE);

         /*T var PointingHeadingArea = WorkingHeadingRectangle;
         var PointingDetailsArea = this.ManipulatingDetailsRectangle; */

         // Start drawings creation
         var DrwFrmPointingHeadingPanel = new DrawingGroup
         {
            Opacity = FrmOpacity
         };

         DrwFrmPointingHeadingPanel.Children.Add(new GeometryDrawing(FrmStroke, FrmPencil, new RectangleGeometry(PointingHeadingArea)));

         //-? if (this.ManipulatedSymbol.CanShowDefinitionOverTop || this.ManipulatedSymbol.CanShowNameOverCenter)
         // DrwFrmPointingHeadingPanel.Children.Add(new GeometryDrawing(this.FrmStroke, this.FrmPencil, new RectangleGeometry(this.ManipulatedSymbol.LabelingArea)));

         FrmPointingHeadingPanel = DrwFrmPointingHeadingPanel.RenderToDrawingVisual();
         Indicators.Add(FrmPointingHeadingPanel);

         // IMPORTANT: The Details related indicators MUST BE CREATED in order to not be mismatched with null in later evaluations
         var DrwFrmPointingDetailsPanel = new DrawingGroup
         {
            Opacity = DrwFrmPointingHeadingPanel.Opacity
         };

         DrwFrmPointingDetailsPanel.Children.Add(new GeometryDrawing(FrmStroke, FrmPencil, new RectangleGeometry(PointingDetailsArea)));
         FrmPointingDetailsPanel = DrwFrmPointingDetailsPanel.RenderToDrawingVisual();

         if (ManipulatedSymbol.AreDetailsShown)
            Indicators.Add(FrmPointingDetailsPanel);

         if (!OnlyAdornAsSelected && WorkingHeadingRectangle.Width >= EDITAREA_MIN_WIDTH
                                  && WorkingHeadingRectangle.Height >= EDITAREA_MIN_HEIGHT)
         {
            var DrwFrmEditPanel = new DrawingGroup
            {
               Opacity = FrmOpacityEdit
            };

            var EditingArea = new Rect(WorkingHeadingRectangle.X + WorkingHeadingRectangle.Width * 0.25,
                                       WorkingHeadingRectangle.Y + WorkingHeadingRectangle.Height * 0.25,
                                       WorkingHeadingRectangle.Width * 0.5,
                                       WorkingHeadingRectangle.Height * 0.5);

            DrwFrmEditPanel.Children.Add(new GeometryDrawing(FrmStrokeEdit, FrmPencil, new RectangleGeometry(EditingArea)));
            FrmEditZone = DrwFrmEditPanel.RenderToDrawingVisual();
            Indicators.Add(FrmEditZone);
            ExclusivePointingIndicators.Add(FrmEditZone);
         }

         if (!ManipulatedSymbol.IsHidden)
         {
            CreateActioners(OnlyAdornAsSelected, PointingHeadingArea, WorkingHeadingRectangle);

            CreateSelectionIndicators();
         }

         // Needed in order to show this adorner's indicators on top of a potentially selected visual element
         RefreshAdorner();
      }

      private void VisualizeOtherAffectedObjects()
      {
         var IncludeOriginatedSubtree = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
         var IncludeTargetedSubtree = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
         var OtherAffectedObjects = OwnerManager.OwnerView.GetCurrentManipulableObjects(IncludeOriginatedSubtree, IncludeTargetedSubtree, true)
                                             .Select(obj => obj.Item1).Distinct()   // Notice that duplicates are informed, so they must be excluded
                                             .Except(ManipulatedObject.IntoEnumerable());   // Exclude current object to avoid visual interference in resizing

         foreach (var AffectedObject in OtherAffectedObjects)
         {
            var DeltaX = 0.0;
            var DeltaY = 0.0;
            var DeltaWidth = 0.0;
            var DeltaHeight = 0.0;

            // Do not consider other intention, particularly resizing (due to its implicit move)
            if (IntendedAction == ESymbolManipulationAction.Move)
            {
               DeltaX = ManipulatingHeadingRectangle.X - ManipulatedSymbol.BaseArea.X;
               DeltaY = ManipulatingHeadingRectangle.Y - ManipulatedSymbol.BaseArea.Y;
            }

            // Only the manipulated symbol show a change in size
            // (because multi-resize is postponed due to difficulty)
            if (ManipulatedSymbol == AffectedObject)
            {
               DeltaWidth = ManipulatingHeadingRectangle.Width - ManipulatedSymbol.BaseArea.Width;
               DeltaHeight = ManipulatingHeadingRectangle.Height - ManipulatedSymbol.BaseArea.Height;
            }

            Rect SelectionZone = AffectedObject is VisualSymbol symbol && symbol.IsHidden
                                 ? new Rect(AffectedObject.BaseCenter.X - 4, AffectedObject.BaseCenter.Y - 4,
                                            8, 8)
                                 : new Rect(AffectedObject.BaseLeft + DeltaX,
                                            AffectedObject.BaseTop + DeltaY,
                                            (AffectedObject.BaseWidth + DeltaWidth).EnforceMinimum(4),
                                            (AffectedObject.TotalArea.Height + DeltaHeight).EnforceMinimum(4));
            var SelectionGeom = (SelectionZone.Width > 8 && SelectionZone.Height > 8
                                 ? new CombinedGeometry(GeometryCombineMode.Exclude,
                                                        new RectangleGeometry(SelectionZone),
                                                        new RectangleGeometry(new Rect(SelectionZone.X + 4, SelectionZone.Y + 4,
                                                                                       (SelectionZone.Width - 8).EnforceMinimum(4),
                                                                                       (SelectionZone.Height - 8).EnforceMinimum(4))))
                                 : (new RectangleGeometry(SelectionZone)) as Geometry);
            var SelectionDrawing = new GeometryDrawing(FrmStroke, FrmPencil, SelectionGeom);
            var SelectionVisual = SelectionDrawing.RenderToDrawingVisual();
 
            SelectionVisual.Opacity = (AffectedObject.IsIn(OwnerManager.OwnerView.SelectedObjects)
                                       ? FrmOpacity : FrmOpacity / 2.0);
            Indicators.Add(SelectionVisual);
         }
      }

      private void CreateActioners(bool OnlyAdornAsSelected, Rect PointingHeadingArea, Rect WorkingHeadingRectangle)
      {
         double PosX;
         double PosY;

         var CanShowActioners = !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                                  Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                                  Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));

         if (!OnlyAdornAsSelected)
         {
            FrmMarkingZones.Clear();

            var ZoneBrush = Brushes.Transparent;                // FrmStrokeEdit (too dark)
            var ZonePen = new Pen(Brushes.Transparent, 1.0);    // FrmPencil

            // Editing Areas of individual shown Details
            var ManipulatedShape = ManipulatedSymbol as VisualShape;

            if (ManipulatedShape != null && ManipulatedShape.CurrentMarkingZones != null)
               CreateMarkingZones(ZoneBrush, ZonePen, ManipulatedShape);

            if (ManipulatedSymbol.AreDetailsShown
                && WorkingHeadingRectangle.Width >= ACTIONER_SIZE
                && WorkingHeadingRectangle.Height >= ACTIONER_SIZE)
            {
               FrmDetailTitleDesignateZones.Clear();
               FrmDetailTitleExpandZones.Clear();
               FrmDetailContentAssignZones.Clear();
               FrmDetailContentEditZones.Clear();

               if (ManipulatedShape != null && ManipulatedShape.CurrentDetailZones != null
                   && ManipulatedShape.CurrentDetailZones.Count > 0)
                  CreateDetailZones(ZoneBrush, ZonePen, ManipulatedShape);

               if (CanShowActioners
                   && ManipulatingDetailsRectangle.Width >= ACTIONER_SIZE * 4.2
                   && ManipulatingDetailsRectangle.Height >= ACTIONER_SIZE * 1.2)
               {
                  // Details Bottom-Left
                  PosX = (PointingHeadingArea.Left + (PointingHeadingArea.Width / 5.0));
                  PosY = ManipulatingDetailsRectangle.Y + ManipulatingDetailsRectangle.Height - ACTIONER_SIZE / 2.0;

                  if (ManipulatedSymbol.OwnerRepresentation.RepresentedIdea.IdeaDefinitor.IsComposable)
                  {
                     ActSwitchCompositeView = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionShowCompositeAsDetail);
                     Indicators.Add(ActSwitchCompositeView);
                     ExclusivePointingIndicators.Add(ActSwitchCompositeView);
                  }

                  // Details Bottom-Right
                  PosX = (PointingHeadingArea.Left + (PointingHeadingArea.Width / 5.0) * 4.0) - ACTIONER_SIZE;
                  ActAddDetail = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionAddDetail);
                  Indicators.Add(ActAddDetail);
                  ExclusivePointingIndicators.Add(ActAddDetail);
               }
            }

            if (CanShowActioners
                && WorkingHeadingRectangle.Width >= ACTIONER_SIZE * 4.2
                && WorkingHeadingRectangle.Height >= ACTIONER_SIZE * 1.2)
            {
               // Top-Left
               PosX = (PointingHeadingArea.Left + (PointingHeadingArea.Width / 5.0));
               PosY = PointingHeadingArea.Top - ACTIONER_SIZE / 2.0;

               DefaultActionIndicator = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionEditProperties);
               Indicators.Add(DefaultActionIndicator);
               ExclusivePointingIndicators.Add(DefaultActionIndicator);

               // Bottom-Left
               PosY = PointingHeadingArea.Top + PointingHeadingArea.Height - ACTIONER_SIZE / 2.0;
               ActShowComposite = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionShowCompositeAsView);

               if (ManipulatedSymbol.OwnerRepresentation.RepresentedIdea.IdeaDefinitor.IsComposable)
               {
                  Indicators.Add(ActShowComposite);
                  ExclusivePointingIndicators.Add(ActShowComposite);
               }

               // Top-Right
               PosX = (PointingHeadingArea.Left + (PointingHeadingArea.Width / 5.0) * 4.0) - ACTIONER_SIZE;
               PosY = PointingHeadingArea.Top - ACTIONER_SIZE / 2.0;
               ActSwitchRelated = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionSwitchRelated);

               Indicators.Add(ActSwitchRelated);
               ExclusivePointingIndicators.Add(ActSwitchRelated);

               // Bottom-Right
               PosY = PointingHeadingArea.Top + PointingHeadingArea.Height - ACTIONER_SIZE / 2.0;
               ActSwitchDetails = CreateActionerFor(PosX, PosY, ESymbolManipulationAction.ActionSwitchDetails);

               Indicators.Add(ActSwitchDetails);
               ExclusivePointingIndicators.Add(ActSwitchDetails);
            }
         }
      }

      private void CreateSelectionIndicators()
      {
         var SelectionIndicators =
                 ManipulatedSymbol.GenerateSelectionIndicators(VisualSymbol.INDICATOR_SIZE,
                                                                     (ManipulatedSymbol.OwnerRepresentation.IsSelected
                                                                      ? VisualElement.SelectionIndicatorBackground
                                                                      : IndStroke),
                                                                     IndPencil,
                                                                     (ManipulatedSymbol.OwnerRepresentation.IsSelected
                                                                      ? VisualElement.SelectionIndicatorGeometryCreator
                                                                      : null));

         foreach (var SelInd in SelectionIndicators)
         {
            var IndVis = SelInd.Item1.RenderToDrawingVisual();

            Indicators.Add(IndVis);

            if (SelInd.Item2)   // Is for Symbol Header...
            {
               if (SelInd.Item3 == EManipulationDirection.Top) IndHeadingTop = IndVis;
               if (SelInd.Item3 == EManipulationDirection.Bottom) IndHeadingBottom = IndVis;
               if (SelInd.Item3 == EManipulationDirection.Left) IndHeadingLeft = IndVis;
               if (SelInd.Item3 == EManipulationDirection.Right) IndHeadingRight = IndVis;

               if (SelInd.Item3 == EManipulationDirection.TopLeft) IndHeadingTopLeft = IndVis;
               if (SelInd.Item3 == EManipulationDirection.TopRight) IndHeadingTopRight = IndVis;
               if (SelInd.Item3 == EManipulationDirection.BottomLeft) IndHeadingBottomLeft = IndVis;
               if (SelInd.Item3 == EManipulationDirection.BottomRight) IndHeadingBottomRight = IndVis;
            }
            else    // Is for Symbol Detail...
            {
               if (SelInd.Item3 == EManipulationDirection.Bottom) IndDetailsBottom = IndVis;
               if (SelInd.Item3 == EManipulationDirection.Left) IndDetailsLeft = IndVis;
               if (SelInd.Item3 == EManipulationDirection.Right) IndDetailsRight = IndVis;
               if (SelInd.Item3 == EManipulationDirection.BottomLeft) IndDetailsBottomLeft = IndVis;
               if (SelInd.Item3 == EManipulationDirection.BottomRight) IndDetailsBottomRight = IndVis;
            }
         }
      }

      private void CreateDetailZones(SolidColorBrush ZoneBrush, Pen ZonePen, VisualShape ManipulatedShape)
      {
         foreach (var DetailZone in ManipulatedShape.CurrentDetailZones)
            if (DetailZone.Value == null)
               break;
            else
            {
               // Title-Designate zone
               if (DetailZone.Value.Item1 != null && DetailZone.Value.Item1 != Rect.Empty)
               {
                  var DetailTitleNamingZone = (new GeometryDrawing(ZoneBrush, ZonePen, new RectangleGeometry(DetailZone.Value.Item1))).RenderToDrawingVisual();
                  DetailTitleNamingZone.Opacity = FrmOpacityEdit;

                  var EditZone = (new GeometryDrawing(FrmStrokeEdit, FrmPencil, new RectangleGeometry(DetailZone.Value.Item1))).RenderToDrawingVisual();
                  EditZone.Opacity = FrmOpacityEdit;
                  Indicators.Add(EditZone);

                  FrmDetailTitleDesignateZones.Add(DetailTitleNamingZone, DetailZone.Key);
                  Indicators.Add(DetailTitleNamingZone);
               }

               // Title-Expand zone
               if (DetailZone.Value.Item2 != null && DetailZone.Value.Item2 != Rect.Empty)
               {
                  var DetailTitleExpandZone = (new GeometryDrawing(ZoneBrush, ZonePen, new RectangleGeometry(DetailZone.Value.Item2))).RenderToDrawingVisual();
                  DetailTitleExpandZone.Opacity = FrmOpacityEdit;

                  FrmDetailTitleExpandZones.Add(DetailTitleExpandZone, DetailZone.Key);
                  Indicators.Add(DetailTitleExpandZone);
               }

               // Content-Edit zone
               // IMPORTANT: Put this edit-zone before the assign-zone to be on the background of it.
               if (DetailZone.Value.Item4 != null && DetailZone.Value.Item4 != Rect.Empty)
               {
                  var DetailContentEditZone = (new GeometryDrawing(ZoneBrush, ZonePen, new RectangleGeometry(DetailZone.Value.Item4))).RenderToDrawingVisual();
                  DetailContentEditZone.Opacity = FrmOpacityEdit;

                  FrmDetailContentEditZones.Add(DetailContentEditZone, DetailZone.Key);
                  Indicators.Add(DetailContentEditZone);
               }

               // Content-Assign zone
               // IMPORTANT: Put this assign-zone after the edit-zone to be on the foreground of it.
               if (DetailZone.Value.Item3 != null && DetailZone.Value.Item3 != Rect.Empty)
               {
                  var DetailContentAssignZone = (new GeometryDrawing(ZoneBrush, ZonePen, new RectangleGeometry(DetailZone.Value.Item3))).RenderToDrawingVisual();
                  DetailContentAssignZone.Opacity = FrmOpacityEdit;

                  var EditZone = (new GeometryDrawing(FrmStrokeEdit, FrmPencil, new RectangleGeometry(DetailZone.Value.Item3))).RenderToDrawingVisual();
                  EditZone.Opacity = FrmOpacityEdit;
                  Indicators.Add(EditZone);

                  FrmDetailContentAssignZones.Add(DetailContentAssignZone, DetailZone.Key);
                  Indicators.Add(DetailContentAssignZone);
               }
            }
      }

      private void CreateMarkingZones(SolidColorBrush ZoneBrush, Pen ZonePen, VisualShape ManipulatedShape)
      {
         foreach (var MarkingZone in ManipulatedShape.CurrentMarkingZones)
         {
            var IconZone = (new GeometryDrawing(ZoneBrush, ZonePen, new RectangleGeometry(MarkingZone.Item2))).RenderToDrawingVisual();
            IconZone.Opacity = FrmOpacityEdit;

            Indicators.Add(IconZone);
            FrmMarkingZones.Add(IconZone, MarkingZone.Item1);
         }
      }

      public DrawingVisual CreateActionerFor(double PosX, double PosY, ESymbolManipulationAction Manipulation)
      {
         ImageSource Source = null;

         if (Manipulation == ESymbolManipulationAction.ActionShowCompositeAsView)
            Source = ImgSrcSwitchContent ?? Display.GetAppImage("show_composite.png");

         if (Manipulation == ESymbolManipulationAction.ActionEditProperties)
            Source = ImgSrcEditProperties ?? Display.GetAppImage("page_white_edit.png");

         if (Manipulation == ESymbolManipulationAction.ActionSwitchRelated)
            /*if (this.ManipulatedElement.OwnerRepresentation.AreRelatedTargetsShown
                || this.ManipulatedElement.OwnerRepresentation.AreRelatedOriginsShown)
                Source = ImgSrcSwitchRelated ?? Display.GetAppImage("related_collapsed.png");
            else*/
            Source = ImgSrcSwitchRelated ?? Display.GetAppImage("related_expanded.png");

         if (Manipulation == ESymbolManipulationAction.ActionSwitchDetails)
            if (ManipulatedSymbol.AreDetailsShown)
               Source = ImgSrcEditFormat ?? Display.GetAppImage("details_close.png");
            else
               Source = ImgSrcEditFormat ?? Display.GetAppImage("details_open.png");

         if (Manipulation == ESymbolManipulationAction.ActionShowCompositeAsDetail)
            if (ManipulatedSymbol.ShowCompositeContentAsDetails)
               Source = ImgSrcEditProperties ?? Display.GetAppImage("detail_view.png");
            else
               Source = ImgSrcEditProperties ?? Display.GetAppImage("composite_view.png");

         if (Manipulation == ESymbolManipulationAction.ActionAddDetail)
            Source = ImgSrcEditProperties ?? Display.GetAppImage("detail_new.png");

         if (Source == null)
            throw new InternalAnomaly("Actioner is not defined for manipulation-action.", Manipulation);

         var ContainerArea = new Rect(PosX, PosY, ACTIONER_SIZE, ACTIONER_SIZE);
         var ContentArea = new Rect(PosX + 2, PosY + 2, ACTIONER_SIZE - 4, ACTIONER_SIZE - 4);
         var Drawer = new DrawingGroup();

         Drawer.Children.Add(new GeometryDrawing(ActStroke, ActPencil, new RectangleGeometry(ContainerArea, 2, 2)));
         Drawer.Children.Add(new ImageDrawing(Source, ContentArea));
         Drawer.Opacity = 1.0;

         return Drawer.RenderToDrawingVisual();
      }

      public MarkerAssignment LastPointedMarkerAssignment { get; protected set; }
      public DetailDesignator LastPointedDetailDesignator { get; protected set; }

      //------------------------------------------------------------------------------------------------------------------------
      public override Visual DeterminePointedVisual(Point Position)
      {
         PreviousPosition = CurrentPosition;
         CurrentPosition = Position;

         if (CurrentPosition == PreviousPosition || IsManipulating)
            return CurrentPointedVisual;

         var NewPointed = GetPointedVisual(Position);

         IsManipulatingHeading = !NewPointed.IsOneOf(FrmPointingDetailsPanel, IndDetailsBottom, IndDetailsBottomLeft, IndDetailsBottomRight, IndDetailsLeft, IndDetailsRight);

         if (NewPointed != CurrentPointedVisual)
         {
            CurrentPointedVisual = NewPointed;

            bool IsPointingToIndicator = true;

            if (NewPointed.IsOneOf(IndHeadingTop, IndHeadingBottom, IndDetailsBottom))
            {
               ResizingDirection = (NewPointed == IndHeadingTop ? EManipulationDirection.Top : EManipulationDirection.Bottom);
               TentativeAction = ESymbolManipulationAction.Resize;
               Cursor = Cursors.SizeNS;
            }
            else if (NewPointed.IsOneOf(IndHeadingLeft, IndHeadingRight, IndDetailsLeft, IndDetailsRight))
            {
               ResizingDirection = (NewPointed.IsOneOf(IndHeadingLeft, IndDetailsLeft) ? EManipulationDirection.Left : EManipulationDirection.Right);
               TentativeAction = ESymbolManipulationAction.Resize;
               Cursor = Cursors.SizeWE;
            }
            else if (NewPointed.IsOneOf(IndHeadingTopLeft, IndHeadingBottomRight, IndDetailsBottomRight))
            {
               ResizingDirection = (NewPointed == IndHeadingTopLeft ? EManipulationDirection.TopLeft : EManipulationDirection.BottomRight);
               TentativeAction = ESymbolManipulationAction.Resize;
               Cursor = Cursors.SizeNWSE;
            }
            else if (NewPointed.IsOneOf(IndHeadingBottomLeft, IndHeadingTopRight, IndDetailsBottomLeft))
            {
               ResizingDirection = (NewPointed == IndHeadingTopRight ? EManipulationDirection.TopRight : EManipulationDirection.BottomLeft);
               TentativeAction = ESymbolManipulationAction.Resize;
               Cursor = Cursors.SizeNESW;
            }
            else if (NewPointed.IsOneOf(FrmPointingHeadingPanel, FrmPointingDetailsPanel))
            {
               TentativeAction = ESymbolManipulationAction.Move;
               Cursor = Cursors.ScrollAll;
            }
            else if (NewPointed == FrmEditZone || NewPointed == null)
            {
               if (ManipulatedSymbol.OwnerRepresentation.IsShortcut)
               {
                  TentativeAction = ESymbolManipulationAction.GoToShortcutTarget;
                  Cursor = Cursors.UpArrow;
               }
               else
               {
                  // Do not Edit In-Place if multiselecting...
                  if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                      Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                  {
                     TentativeAction = ESymbolManipulationAction.Move;
                     Cursor = Cursors.ScrollAll;
                  }
                  else
                  {
                     TentativeAction = ESymbolManipulationAction.EditInPlace;
                     Cursor = Cursors.Pen;
                  }
               }
            }
            else if (NewPointed.IsIn(FrmMarkingZones.Keys))
            {
               LastPointedMarkerAssignment = FrmMarkingZones[NewPointed];
               TentativeAction = ESymbolManipulationAction.MarkerEdit;
               Cursor = Cursors.Pen;
            }
            else if (NewPointed.IsIn(FrmDetailContentAssignZones.Keys))
            {
               LastPointedDetailDesignator = FrmDetailContentAssignZones[NewPointed];
               TentativeAction = ESymbolManipulationAction.IndividualDetailChange;
               Cursor = Cursors.Hand;
            }
            else if (NewPointed.IsIn(FrmDetailContentEditZones.Keys))
            {
               LastPointedDetailDesignator = FrmDetailContentEditZones[NewPointed];
               TentativeAction = ESymbolManipulationAction.IndividualDetailAccess;
               Cursor = Cursors.Pen;
            }
            else if (NewPointed.IsIn(FrmDetailTitleExpandZones.Keys))
            {
               LastPointedDetailDesignator = FrmDetailTitleExpandZones[NewPointed];
               TentativeAction = ESymbolManipulationAction.SwitchIndividualDetail;
               Cursor = Cursors.UpArrow;
            }
            else if (NewPointed.IsIn(FrmDetailTitleDesignateZones.Keys))
            {
               LastPointedDetailDesignator = FrmDetailTitleDesignateZones[NewPointed];
               TentativeAction = ESymbolManipulationAction.IndividualDetailDesignation;
               Cursor = Cursors.Arrow;
            }
            else
            {
               Cursor = Cursors.Hand;

               if (NewPointed == ActSwitchDetails)
                  TentativeAction = ESymbolManipulationAction.ActionSwitchDetails;
               else if (NewPointed == ActSwitchRelated)
                  TentativeAction = ESymbolManipulationAction.ActionSwitchRelated;
               else if (NewPointed == ActShowComposite)
                  TentativeAction = ESymbolManipulationAction.ActionShowCompositeAsView;
               else if (NewPointed == DefaultActionIndicator)
                  TentativeAction = ESymbolManipulationAction.ActionEditProperties;
               else if (NewPointed == ActSwitchCompositeView)
                  TentativeAction = ESymbolManipulationAction.ActionShowCompositeAsDetail;
               else if (NewPointed == ActAddDetail)
                  TentativeAction = ESymbolManipulationAction.ActionAddDetail;
               else
                  IsPointingToIndicator = false;
            }

            if (IsPointingToIndicator)
            {
               var IndDescription = TentativeAction.GetDescription();

               if (TentativeAction == ESymbolManipulationAction.EditInPlace
                   && ManipulatedSymbol.OwnerRepresentation is RelationshipVisualRepresentation representation)
               {
                  var RepRel = representation.RepresentedRelationship;

                  if (!RepRel.RelationshipDefinitor.Value.IsSimple || RepRel.Links.Count <= 1)
                     IndDescription += " Drag to extend the pointed Relationship with a new Link/Connector.";
               }

               ProductDirector.ShowAssistance(IndDescription);

               /* Problem: This tooltip stops the adorner working
               var Tip = this.ToolTip as ToolTip;

               if (Tip == null || (Tip.Content as string).IsAbsent())
               {
                   Tip = (Tip == null ? new ToolTip() : Tip);
                   this.ToolTip = Tip;

                   Tip.Content = IndDescription;
                   Tip.IsOpen = true;
                   Tip.StaysOpen = false;
               } */
            }
            else
               ProductDirector.ShowAssistance();

            ProductDirector.ShowPointingTo(ManipulatedSymbol);
         }

         return NewPointed;
      }

      //------------------------------------------------------------------------------------------------------------------------
      protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
      {
         base.OnMouseRightButtonUp(e);

         OwnerManager.OwnerView.Engine.ShowContextMenu(OwnerManager.OwnerView.Presenter, ManipulatedSymbol, OwnerManager.OwnerView);
      }

      //------------------------------------------------------------------------------------------------------------------------
   }
}