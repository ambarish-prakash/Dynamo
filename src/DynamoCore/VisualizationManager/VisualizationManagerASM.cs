﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Media3D;
using Autodesk.LibG;
using Dynamo.Models;
using Dynamo.Selection;
using HelixToolkit.Wpf;

namespace Dynamo
{
    public class VisualizationManagerASM : VisualizationManager
    {
        protected override void VisualizationUpdateThread(object s, DoWorkEventArgs args)
        {
            //only update those nodes which have been flagged for update
            var toUpdate = Visualizations.Values.ToList().Where(x => x.RequiresUpdate == true);

            Debug.WriteLine(string.Format("{0} visualizations to update", toUpdate.Count()));
            Debug.WriteLine(string.Format("Updating visualizations on thread {0}.", System.Threading.Thread.CurrentThread.ManagedThreadId));

            var selIds =
                DynamoSelection.Instance.Selection.Where(x => x is NodeModel)
                               .Select(x => ((NodeModel)x).GUID.ToString());

            var selected = Visualizations.Where(x => selIds.Contains(x.Key)).Select(x => x.Value);

            var sw = new Stopwatch();
            sw.Start();

            foreach (var n in toUpdate)
            {
                var rd = n.Description;
                rd.Clear();

                foreach (var geom in n.Geometry.ToList())
                {
                    var g = geom as GraphicItem;
                    if (g == null)
                        continue;

                    DrawLibGGraphicItem(g, rd, selected, n);

                    //set this flag to avoid processing again
                    //if not necessary
                    n.RequiresUpdate = false;
                }
            }

            sw.Stop();
            Debug.WriteLine(string.Format("{0} elapsed for generating visualizations.", sw.Elapsed));
            DynamoLogger.Instance.Log(string.Format("{0} elapsed for generating visualizations.", sw.Elapsed));

            OnVisualizationUpdateComplete(this, EventArgs.Empty);
        }

        public static void DrawLibGGraphicItem(GraphicItem g, RenderDescription rd, IEnumerable<Visualization> selected, Visualization n)
        {

            if (g is CoordinateSystem)
            {
                #region draw coordinate systems

                var line_strip_vertices = g.line_strip_vertices_threadsafe();

                for (int i = 0; i < line_strip_vertices.Count; i += 6)
                {
                    var p1 = new Point3D(
                        line_strip_vertices[i],
                        line_strip_vertices[i + 1],
                        line_strip_vertices[i + 2]);

                    var p2 = new Point3D(
                        line_strip_vertices[i + 3],
                        line_strip_vertices[i + 4],
                        line_strip_vertices[i + 5]);

                    if (i < 6)
                    {
                        rd.XAxisPoints.Add(p1);
                        rd.XAxisPoints.Add(p2);
                    }
                    else if (i >= 6 && i < 12)
                    {
                        rd.YAxisPoints.Add(p1);
                        rd.YAxisPoints.Add(p2);
                    }
                    else
                    {
                        rd.ZAxisPoints.Add(p1);
                        rd.ZAxisPoints.Add(p2);
                    }
                }

                #endregion
            }
            else
            {
                #region draw points

                var point_vertices = g.point_vertices_threadsafe();

                var selArray = selected as Visualization[] ?? selected.ToArray();

                for (int i = 0; i < point_vertices.Count; i += 3)
                {
                    if (selArray.Contains(n))
                    {
                        rd.SelectedPoints.Add(new Point3D(point_vertices[i],
                                                          point_vertices[i + 1], point_vertices[i + 2]));
                    }
                    else
                    {
                        rd.Points.Add(new Point3D(point_vertices[i],
                                                  point_vertices[i + 1], point_vertices[i + 2]));
                    }
                }

                #endregion

                #region draw lines

                SizeTList num_line_strip_vertices = g.num_line_strip_vertices_threadsafe();
                FloatList line_strip_vertices = g.line_strip_vertices_threadsafe();

                int counter = 0;

                foreach (uint num_verts in num_line_strip_vertices)
                {
                    for (int i = 0; i < num_verts; ++i)
                    {
                        var p = new Point3D(
                            line_strip_vertices[counter],
                            line_strip_vertices[counter + 1],
                            line_strip_vertices[counter + 2]);

                        if (selArray.Contains(n))
                        {
                            rd.SelectedLines.Add(p);
                        }
                        else
                        {
                            rd.Lines.Add(p);
                        }

                        counter += 3;

                        if (i == 0 || i == num_verts - 1)
                            continue;

                        if (selArray.Contains(n))
                        {
                            rd.SelectedLines.Add(p);
                        }
                        else
                        {
                            rd.Lines.Add(p);
                        }
                    }
                }

                #endregion

                #region draw surface

                //var sw = new Stopwatch();
                //sw.Start();

                var builder = new MeshBuilder();

                FloatList triangle_vertices = g.triangle_vertices_threadsafe();
                FloatList triangle_normals = g.triangle_normals_threadsafe();
                
                for (int i = 0; i < triangle_vertices.Count; i+=3)
                {
                    var new_point = new Point3D(triangle_vertices[i],
                                                triangle_vertices[i + 1],
                                                triangle_vertices[i + 2]);

                    var normal = new Vector3D(triangle_normals[i],
                                                triangle_normals[i + 1],
                                                triangle_normals[i + 2]);

                    builder.TriangleIndices.Add(builder.Positions.Count);
                    builder.Normals.Add(normal);
                    builder.Positions.Add(new_point);
                    builder.TextureCoordinates.Add(new System.Windows.Point(0, 0));
                }

                //sw.Stop();
                //Debug.WriteLine(string.Format("{0} elapsed for drawing geometry.", sw.Elapsed));

                //don't add empty meshes
                if (builder.Positions.Count > 0)
                {
                    if (selArray.Contains(n))
                    {
                        rd.SelectedMeshes.Add(builder.ToMesh(true));
                    }
                    else
                    {
                        rd.Meshes.Add(builder.ToMesh(true));
                    }
                }

                #endregion
            }
        }
    }
}