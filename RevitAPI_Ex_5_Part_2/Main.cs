using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RevitAPI_Ex_5_Part_2.Main;

namespace RevitAPI_Ex_5_Part_2
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        private Document doc;
        private static string levelDown;
        private static string levelHigh;
        private static string doorName;
        private static string doorFamilyName;
        private static string windowName;
        private static string windowFamilyName;
        private static int i;
        public List<Level> listLevel { get; set; } = new List<Level>();
        public List<Wall> walls { get; set; } = new List<Wall>();
        public List<FamilySymbol> listDoors { get; set; } = new List<FamilySymbol>();
        public List<FamilySymbol> listWindows { get; set; } = new List<FamilySymbol>();
        public double Width { get; private set; }
        public double Depth { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            doc = commandData.Application.ActiveUIDocument.Document;
            levelDown = "Уровень 1";
            levelHigh = "Уровень 2";
            doorName = "0915 x 2032 мм";
            doorFamilyName = "Одиночные-Щитовые";
            windowName = "0915 x 1220 мм";
            windowFamilyName = "Фиксированные";
            Width = 10000;
            Depth = 10000;
            listLevel = ListLevels.GetLevels(commandData);
            listDoors = ListDoors.GetDoors(commandData);
            listWindows = ListWindows.GetWindows(commandData);
            WallsCreate();

            return Result.Succeeded;
        }

        private void WallsCreate()
        {
            double width = UnitUtils.ConvertToInternalUnits(Width, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(Depth, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            Transaction ts = new Transaction(doc, "Создание стен");
            ts.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, listLevel[0].Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(listLevel[1].Id);
            }

            AddDoors();
            i = 0;
            foreach (Wall wall in walls)
            {
                AddWindows();
                i++;
            }
            ts.Commit();
        }

        private void AddWindows()
        {
            LocationCurve hostCurve = walls[i].Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = point2 + (point1 - point2) * 0.8;
            XYZ point_1 = point2 + (point1 - point2) * 0.3;
            point = new XYZ(point.X, point.Y, point.Z + 3.0);
            point_1 = new XYZ(point_1.X, point_1.Y, point_1.Z + 3.0);
            if (!listWindows[0].IsActive)
                listWindows[0].Activate();
            doc.Create.NewFamilyInstance(point, listWindows[0], walls[i], listLevel[0], StructuralType.NonStructural);
            doc.Create.NewFamilyInstance(point_1, listWindows[0], walls[i], listLevel[0], StructuralType.NonStructural);
        }

        private void AddDoors()
        {
            LocationCurve hostCurve = walls[0].Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point2 + point1) / 2;
            if (!listDoors[0].IsActive)
                listDoors[0].Activate();
            doc.Create.NewFamilyInstance(point, listDoors[0], walls[0], listLevel[0], StructuralType.NonStructural);
        }

        public class ListLevels
        {
            public static List<Level> GetLevels(ExternalCommandData commandData)
            {
                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<Level> listLevel = new FilteredElementCollector(doc)
                 .OfClass(typeof(Level))
                 .OfType<Level>()
                 .Where(x => x.Name.Equals(levelDown) || x.Name.EndsWith(levelHigh))
                 .ToList();
                return listLevel;
            }
        }

        public class ListDoors
        {
            public static List<FamilySymbol> GetDoors(ExternalCommandData commandData)
            {
                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                List<FamilySymbol> listDoors = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Doors)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals(doorName))
                 .Where(x => x.FamilyName.Equals(doorFamilyName))
                 .ToList();
                return listDoors;
            }
        }

        public class ListWindows
        {
            public static List<FamilySymbol> GetWindows(ExternalCommandData commandData)
            {
                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                List<FamilySymbol> listWindows = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Windows)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals(windowName))
                 .Where(x => x.FamilyName.Equals(windowFamilyName))
                 .ToList();
                return listWindows;
            }
        }
    }
}
