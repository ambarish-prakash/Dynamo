﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.DesignScript.Geometry;
using RevitServices.Persistence;
using Line = Autodesk.Revit.DB.Line;
using System.ComponentModel;

namespace DSRevitNodes.GeometryConversion
{
    [Browsable(false)]
    public static class Extensions
    {

        #region Proto -> Revit types

        /// <summary>
        /// Convert a Point to an XYZ
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static Autodesk.Revit.DB.XYZ ToXyz(this Autodesk.DesignScript.Geometry.Point pt)
        {
            return new XYZ(pt.X, pt.Y, pt.Z);
        }

        /// <summary>
        /// Convert a Vector to an XYZ.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Autodesk.Revit.DB.XYZ ToXyz(this Vector vec)
        {
            return new XYZ(vec.X, vec.Y, vec.Z);
        }

        /// <summary>
        /// Convert a ReferencePoint to an XYZ.
        /// </summary>
        /// <param name="refPt"></param>
        /// <returns></returns>
        public static Autodesk.Revit.DB.XYZ ToXyz(this Autodesk.Revit.DB.ReferencePoint refPt)
        {
            return refPt.Position;
        }

        /// <summary>
        /// Convert a CoordinateSystem to a Transform
        /// </summary>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static Autodesk.Revit.DB.Transform ToTransform(this CoordinateSystem cs)
        {
            var trans = Transform.Identity;
            trans.Origin = cs.Origin.ToXyz();
            trans.BasisX = cs.XAxis.ToXyz();
            trans.BasisY = cs.YAxis.ToXyz();
            trans.BasisZ = cs.ZAxis.ToXyz();
            return trans;
        }

        /// <summary>
        /// Convert a DS Plane to a Revit Plane.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Autodesk.Revit.DB.Plane ToPlane(this Autodesk.DesignScript.Geometry.Plane plane)
        {
            return new Autodesk.Revit.DB.Plane(plane.Normal.ToXyz(), plane.Origin.ToXyz());
        }

        /// <summary>
        /// Convert list of points to list of xyz's
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns></returns>
        public static List<XYZ> ToXyzs(this List<Autodesk.DesignScript.Geometry.Point> list)
        {
            return list.ConvertAll((x) => new XYZ(x.X, x.Y, x.Z));
        }

        /// <summary>
        /// Convert array of points to list of xyz's
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns></returns>
        public static XYZ[] ToXyzs(this Autodesk.DesignScript.Geometry.Point[] list)
        {
            return list.ToList().ConvertAll((x) => new XYZ(x.X, x.Y, x.Z)).ToArray();
        }

        /// <summary>
        /// Convert array of points to list of xyz's
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns></returns>
        public static DoubleArray ToDoubleArray(this double[] list)
        {
            var n = new DoubleArray();
            list.ToList().ForEach(x => n.Append(ref x));
            return n;
        }

        /// <summary>
        /// Convert a 2d array of doubles into an array of UV
        /// </summary>
        /// <param name="uvArr"></param>
        /// <returns></returns>
        public static UV[] ToUvs(this double[][] uvArr)
        {
            var uvs = new UV[uvArr.Length];
            var count = 0;
            foreach (var row in uvArr)
            {
                if (row.Length != 2)
                {
                    throw new Exception("Each element of the input array should be length 2");
                }
                else
                {
                    uvs[count++] = new Autodesk.Revit.DB.UV(row[0], row[1]);
                }
            }

            return uvs;
        }

        #endregion

        #region Revit -> Proto types

        /// <summary>
        /// Convert an XYZ to a Point
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Point ToPoint(this XYZ xyz )
        {
            return Autodesk.DesignScript.Geometry.Point.ByCoordinates(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Convert an XYZ to a Vector.
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static Vector ToVector(this XYZ xyz)
        {
            return Vector.ByCoordinates(xyz.X, xyz.Y, xyz.Z);
        }


        /// <summary>
        /// Convert a Revit Plane to a DS Plane.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Plane ToPlane(this Autodesk.Revit.DB.Plane plane)
        {
            return Autodesk.DesignScript.Geometry.Plane.ByOriginNormal(plane.Origin.ToPoint(), plane.Normal.ToVector());
        }


        /// <summary>
        /// Convert a Transform to a CoordinateSystem
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static CoordinateSystem ToCoordinateSystem(this Transform t)
        {
            return CoordinateSystem.ByOriginVectors(t.Origin.ToPoint(), t.BasisX.ToVector(), t.BasisY.ToVector());
        }

        /// <summary>
        /// Convert Revit Line to DS Line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Line ToLine(this Autodesk.Revit.DB.Line line)
        {
            return Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(
                line.get_EndPoint(0).ToPoint(), line.get_EndPoint(1).ToPoint());
        }

        /// <summary>
        /// Convert list of points to list of xyz's
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns></returns>
        public static List<Autodesk.DesignScript.Geometry.Point> ToPoints(this List<XYZ> list)
        {
            return list.ConvertAll((x) => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x.X, x.Y, x.Z));
        }

        #endregion

    }
}