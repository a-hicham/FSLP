﻿using Visio = Microsoft.Office.Interop.Visio;
using System;

namespace SqpiLand
{
    internal class VisioDrawer
    {
        private static System.Collections.Generic.Dictionary<string, int> tablesID = new System.Collections.Generic.Dictionary<string, int>();
        private static System.Collections.Generic.Dictionary<string, int> fieldsID = new System.Collections.Generic.Dictionary<string, int>();
        private static Visio.Shape fromField;
        private static Visio.Shape toField;
        private static Visio.Application application;
        private static Visio.Document doc;
        private static Visio.Page page;
        private static Visio.Master visioEntityMaster;
        private static Visio.Master visioAttributeMaster;
        private static Visio.Master visioConnectorMaster;

        public static void DrawModel(Model.DBModel model, bool withViews, bool showVisio, string path, bool physNames, bool allFields)
        {
            if(!System.IO.File.Exists(path + @"Files\SLModel.VSDX"))
            {
                System.Windows.MessageBox.Show("Das Template SLModel.vsdx wurde nicht unter " + path + @"Files\ gefunden. Bitte die README-Datei lesen.");
                return;
            }

            try
            {
                var x = -5;
                var y = 11;
                var breite = (int)Math.Sqrt(model.TablesList.Count);
                var hoehe = 0;
                var counter = 0;

                application = new Visio.Application();
                application.Visible = showVisio;

                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                doc = application.Documents.Open(path + @"Files\SLModel.VSDX");
                //doc = application.Documents.Open(Environment.CurrentDirectory + @"\Files\SLModel.VSDX");
                doc.SaveAs(path + model.serverName + ".vsdx");
                doc.Creator = @"Hicham Ait Ayad";
                doc.Title = model.serverName;

                page = doc.Pages[1];
                page.Name = @"Scopeland DB Model Builder";
                page.AutoSize = true;
                visioEntityMaster = doc.Masters.get_ItemU(@"Entity");
                visioAttributeMaster = doc.Masters.get_ItemU(@"Attribute");
                visioConnectorMaster = doc.Masters.get_ItemU(@"Relationship");

                foreach (Model.Table table in model.TablesList)
                {
                    if (withViews || !table.KindOfObject.Trim().Equals("View"))
                    {
                        if (counter++ % breite == 0)
                        {
                            y -= hoehe / 2;
                            hoehe = 0;
                            x = -5;
                        }
                        Visio.Shape entity = page.Drop(visioEntityMaster, x += 5, y);

                        if (allFields && table.Fields.Count > hoehe)
                            hoehe = table.Fields.Count;
                        else
                            if (!allFields)
                        {
                            foreach (Model.Field field in table.Fields)
                            {
                                hoehe += model.FieldList.Contains(field) ? 1 : 0;
                            }
                        }
                                

                        Array members = entity.ContainerProperties.GetListMembers();
                        foreach (int member in members)
                            entity.Shapes.ItemFromID[member].Delete();
                        //tablesID.Add(table.Name, entity.ID);
                        //printProperties(entity.Shapes);
                        entity.Text = physNames ? table.Name : table.NameD;
                        int i = 1;


                        foreach (Model.Field field in table.Fields)
                        {
                            if(allFields || model.FieldList.Contains(field))
                            {
                                Visio.Shape attribute = page.Drop(visioAttributeMaster, 0, 0);
                                field.ShapeID = attribute.UniqueID[(short)Visio.VisUniqueIDArgs.visGetOrMakeGUID];
                                //fieldsID.Add(field.Table.Name + "_" + field.Name, attribute.ID);
                                attribute.Text = physNames ? field.Name : field.NameD;
                                entity.ContainerProperties.InsertListMember(attribute, i++);
                                //entity.ContainerProperties.AddMember(visioAttributeMaster, Visio.VisMemberAddOptions.visMemberAddUseResizeSetting);
                            }
                        }
                    }
                }
                page.CreateSelection(Visio.VisSelectionTypes.visSelTypeAll).Layout();

                foreach(Model.Relation relation in model.RelationsList)
                {
                    if(relation.FromField.ShapeID != null && relation.ToField.ShapeID != null && relation.TypeFrom != null && relation.TypeTo != null)
                    {
                        int index;
                        if ((relation.TypeFrom.Substring(3).Equals(">>") && relation.TypeTo.Substring(3).Equals("->")) || (relation.TypeFrom.Substring(3).Equals("->") && relation.TypeTo.Substring(3).Equals(">>")))
                        {
                            fromField = relation.TypeFrom.Substring(3).Equals(">>") ? page.Shapes.ItemFromUniqueID[relation.ToField.ShapeID] : page.Shapes.ItemFromUniqueID[relation.FromField.ShapeID];
                            toField = relation.TypeFrom.Substring(3).Equals(">>") ? page.Shapes.ItemFromUniqueID[relation.FromField.ShapeID] : page.Shapes.ItemFromUniqueID[relation.ToField.ShapeID];
                            index = 0;
                        }
                        else
                        {
                            if(relation.TypeFrom.Substring(3).Equals(">>") && relation.TypeTo.Substring(3).Equals(">>"))
                            {
                                fromField = page.Shapes.ItemFromUniqueID[relation.FromField.ShapeID];
                                toField =  page.Shapes.ItemFromUniqueID[relation.ToField.ShapeID];
                                index = 2;
                            }
                            else
                            {
                                fromField = page.Shapes.ItemFromUniqueID[relation.FromField.ShapeID];
                                toField = page.Shapes.ItemFromUniqueID[relation.ToField.ShapeID];
                                index = 1;
                            }
                        }
                        ConnectWithDynamicGlueAndConnector(fromField, toField, index);
                    }
                }
                page.AutoSizeDrawing();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                doc.Save();
                if(showVisio)
                    application.Quit();
            }
            System.Windows.MessageBox.Show("All done!");
        }

        //Arrows: 0=Standard, 1 = keine, 2= beide
        private static void ConnectWithDynamicGlueAndConnector(Visio.Shape shapeFrom, Visio.Shape shapeTo, int arrows)
        {
           // if (!cubeList.Contains(shapeFrom) || !cubeList.Contains(shapeTo))
           //     return;

            Visio.Cell beginXCell;
            Visio.Cell endXCell;
            Visio.Cell beginArrow;
            Visio.Cell endArrow;
            Visio.Shape connector;

            // Add a Dynamic connector to the page.
            connector = page.Drop(visioConnectorMaster, 0, 0);

            beginArrow = connector.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionObject,
                (short)Visio.VisRowIndices.visRowLine,
                (short)Visio.VisCellIndices.visLineBeginArrow);

            endArrow = connector.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionObject,
                (short)Visio.VisRowIndices.visRowLine,
                (short)Visio.VisCellIndices.visLineEndArrow);

            
            if(arrows == 1)
                endArrow.FormulaU = "0";
            if (arrows == 2)
                beginArrow.FormulaU = "4";

            // Connect the begin point.
            beginXCell = connector.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionObject,
                (short)Visio.VisRowIndices.visRowXForm1D,
                (short)Visio.VisCellIndices.vis1DBeginX);

            beginXCell.GlueTo(shapeFrom.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionConnectionPts,
                (short)1,
                (short)Visio.VisCellIndices.visCnnctX));

            // Connect the end point.
            endXCell = connector.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionObject,
                (short)Visio.VisRowIndices.visRowXForm1D,
                (short)Visio.VisCellIndices.vis1DEndX);

            endXCell.GlueTo(shapeTo.get_CellsSRC(
                (short)Visio.VisSectionIndices.visSectionConnectionPts,
                (short)1,
                (short)Visio.VisCellIndices.visCnnctX));
        }


        public static void printProperties(Visio.Shapes shapes)
        {
            
            // Look at each shape in the collection.
            foreach (Visio.Shape shape in shapes)
            {
                // Use this index to look at each row in the properties 
                // section.
                short iRow = (short)Visio.VisRowIndices.visRowFirst;

                // While there are stil rows to look at.
                while (shape.get_CellsSRCExists(
                    (short)Visio.VisSectionIndices.visSectionProp,
                    iRow,
                    (short)Visio.VisCellIndices.visCustPropsValue,
                    (short)0) != 0)
                {
                    // Get the label and value of the current property.
                    string label = shape.get_CellsSRC(
                            (short)Visio.VisSectionIndices.visSectionProp,
                            iRow,
                            (short)Visio.VisCellIndices.visCustPropsLabel
                        ).get_ResultStr(Visio.VisUnitCodes.visNoCast);

                    string value = shape.get_CellsSRC(
                            (short)Visio.VisSectionIndices.visSectionProp,
                            iRow,
                            (short)Visio.VisCellIndices.visCustPropsValue
                        ).get_ResultStr(Visio.VisUnitCodes.visNoCast);

                    // Print the results.
                    System.Windows.MessageBox.Show((string.Format(
                        "Shape={0} Label={1} Value={2}",
                        shape.Name, label, value)));

                    // Move to the next row in the properties section.
                    iRow++;
                }

                // Now look at child shapes in the collection.
                if (shape.Master == null && shape.Shapes.Count > 0)
                    printProperties(shape.Shapes);
            }
        }

        private static void kill()
        {
            doc.Save();
            application.Quit();
        }
    }
}
