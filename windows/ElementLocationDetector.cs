using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Automation;

namespace ClickyWindows;

public class ElementLocationDetector
{
    public static Point? FindElementLocation(string elementDescription, Bitmap screenshot)
    {
        // Basic implementation: use UI Automation to find elements by name or type
        // This is a simplified version - in practice, would need more sophisticated matching

        var root = AutomationElement.RootElement;
        var condition = new PropertyCondition(AutomationElement.NameProperty, elementDescription);
        var element = root.FindFirst(TreeScope.Descendants, condition);

        if (element != null)
        {
            var rect = element.Current.BoundingRectangle;
            return new Point((int)rect.Left + (int)rect.Width / 2, (int)rect.Top + (int)rect.Height / 2);
        }

        // Fallback: search for partial matches
        condition = new PropertyCondition(AutomationElement.NameProperty, elementDescription, PropertyConditionFlags.IgnoreCase);
        element = root.FindFirst(TreeScope.Descendants, condition);

        if (element != null)
        {
            var rect = element.Current.BoundingRectangle;
            return new Point((int)rect.Left + (int)rect.Width / 2, (int)rect.Top + (int)rect.Height / 2);
        }

        return null;
    }

    public static List<Point> FindMultipleElementLocations(string[] descriptions, Bitmap screenshot)
    {
        var points = new List<Point>();
        foreach (var desc in descriptions)
        {
            var point = FindElementLocation(desc, screenshot);
            if (point.HasValue)
            {
                points.Add(point.Value);
            }
        }
        return points;
    }
}