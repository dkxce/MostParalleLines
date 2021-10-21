/****************************************************
*                                                   *
*      C# Line/Polyline Most Parallel Comparer      *
*   Basically usage for snap track(s) to road map   *
*              milokz@gmail.com                     *
*                                                   *
****************************************************/

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace LineComparer
{
    public class LineComparer
    {
        /// <summary>
        ///     Ìàêñèìàëüíîå îòêëîíåíèå ïðè ñîïîñòàâëåíèè âåêòîðîâ â ìåòðàõ (äëÿ ãåîãðàôè÷åñêèõ êîîðäèíàò)
        /// </summary>
        public static int max_vect_error_meters = 15;

        /// <summary>
        ///     Ìàêñèìàëüíîå îòêëîíåíèå ïðè ñîïîñòàâëåíèè âåêòîðîâ â ìåòðàõ (äëÿ òî÷å÷íûõ êîîðäèíàò)
        /// </summary>
        public static int max_vect_error_points = 10;

        /// <summary>
        ///     Ïðîâåðÿåì ñîâïàäàþò ëè âåêòîðû äîðîã/ëèíèé
        /// </summary>
        /// <param name="mainLine">Îñíîâíàÿ ëèíèÿ (îæèäàåìî áîëåå êîðîòêàÿ)</param>
        /// <param name="toCheckLine">Ñðàâíèâàåìàÿ ëèíèÿ (îæèäàåìî áîëåå äëèííàÿ)</param>
        /// <param name="isGeo">Ãåîãðàôè÷åñêèå êîîðäèíàòû</param>
        /// <param name="samedir">Ñîíàïðàâëåíû</param>
        /// <param name="distance">Ðàññòîÿíèå</param>
        /// <param name="crossPoint">Òî÷êà ïåðåñå÷åíèÿ</param>
        /// <returns></returns>
        public static bool isMostParallel(PointF[] mainLine, PointF[] toCheckLine, bool isGeo, out bool samedir, out double distance, out PointF crossPoint)
        {
            samedir = true;
            distance = double.MaxValue;
            crossPoint = default(PointF);

            // Ñòðîèì ïðîåêöèþ îñíîâíîé ëèíèè (mainLine) íà ñîïîñòàâëÿåìóþ (toCheckLine)
            // Îñíîâíàÿ ëèíèÿ ïðåèìóùåñòâåííî äîëæíà áûòü êîðî÷å ñîïîñòàâÿëåìîé
            int[] jj = new int[mainLine.Length]; // ïðîâåðÿåì ñîíàïðàâëåííîñòü
            for (int n = 0; n < mainLine.Length; n++)
            {
                //âñå òî÷êè äîðîãè èç main íå äîëæíû áûòü çà ãðàíèöàìè âñåõ ó÷àñòêîâ toCheckLine
                bool main_inside_check = false;

                //ìèíèìàëüíîå ðàññòîÿíèå äî ïðîåêöèè òî÷êè
                double min_dist2line = double.MaxValue;
                
                for (int j = 1; j < toCheckLine.Length; j++)
                {
                    PointF curr_projPoint;
                    bool curr_projPointOutOfLine;
                    double curr_dist2line = DistanceFromPointToLine(mainLine[n], toCheckLine[j - 1], toCheckLine[j], out curr_projPoint, out curr_projPointOutOfLine, isGeo);

                    if (curr_projPointOutOfLine) // òî÷êà çà ãðàíèöàìè ó÷àñòêà ëèíèè
                        continue;
                    else // òî÷êà ïðîåêöèè ëåæèò íà ó÷àñòêå ëèíèè
                        main_inside_check = true;

                    if (curr_dist2line < min_dist2line)
                    {
                        min_dist2line = curr_dist2line;
                        jj[n] = j;                        
                    };
                };

                distance = min_dist2line; // ìèíèìàëüíàÿ äëèíà ïåðïåíäèêóëÿðà ê ëèíèè

                if (!main_inside_check) return false; // åñëè òî÷êà äîðîãè çà ãðàíèöàìè âñåõ ó÷àñòêîâ ëèíèè - äîðîãè íå ñîâïàäàþò

                if (isGeo)
                {
                    if (min_dist2line > max_vect_error_meters) return false;
                }
                else
                {
                    if (min_dist2line > max_vect_error_points) return false;
                };
            };

            // ïðîâåðÿåì ñîíàïðàâëåííîñòü äîðîã
            if (jj[0] == jj[mainLine.Length - 1]) // åñëè ñîâïàäàåò òîëüêî 1 ó÷àñòîê
            {
                // CHECK ANGLE
                double xa = mainLine[mainLine.Length - 1].X - mainLine[0].X;
                double ya = mainLine[mainLine.Length - 1].Y - mainLine[0].Y;
                double xb = toCheckLine[toCheckLine.Length - 1].X - toCheckLine[0].X;
                double yb = toCheckLine[toCheckLine.Length - 1].Y - toCheckLine[0].Y;

                double ka = ya / xa;
                double kb = yb / xb;
                double ua = Math.Atan(ka);// * 180 / Math.PI;
                double ub = Math.Atan(kb);// * 180 / Math.PI;
                double ud = Math.Abs(ua - ub);

                samedir = ud < (Math.PI / 2.0); // 90 degrees
            }
            else // åñëè ñîâïàäàåò íåñêîëüêî ó÷àñòêîâ
                samedir = jj[0] < jj[mainLine.Length - 1];

            // Ïåðåñåêàþòñÿ ëè 
            for(int a = 1;a<mainLine.Length;a++)
                for (int b = 1; b < toCheckLine.Length; b++)
                {
                    PointF intersection;
                    if (isIntersected(new PointF[] { mainLine[a], mainLine[a-1] }, new PointF[] { toCheckLine[b], toCheckLine[b-1] }, out intersection))
                    {
                        distance = 0;
                        crossPoint = intersection;
                        return true;
                    };
                };

            return true;
        }

        /// <summary>
        ///     Íàõîäèì òî÷êó ïåðåñå÷åíèÿ ëèíèé
        /// </summary>
        /// <param name="twoPointsLineA"></param>
        /// <param name="twoPointsLineB"></param>
        /// <param name="intersection"></param>
        /// <returns></returns>
        private static bool isIntersected(PointF[] twoPointsLineA, PointF[] twoPointsLineB, out PointF intersection)
        {
            intersection = new PointF(0, 0);

            double tolerance = 0.001;
            double x1 = twoPointsLineA[0].X, y1 = twoPointsLineA[0].Y;
            double x2 = twoPointsLineA[1].X, y2 = twoPointsLineA[1].Y;

            double x3 = twoPointsLineB[0].X, y3 = twoPointsLineB[0].Y;
            double x4 = twoPointsLineB[1].X, y4 = twoPointsLineB[1].Y;

            // equations of the form x = c (two vertical lines)
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance && Math.Abs(x1 - x3) < tolerance) return false;

            //equations of the form y=c (two horizontal lines)
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance && Math.Abs(y1 - y3) < tolerance) return false;

            //equations of the form x=c (two vertical parallel lines)
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance) return false;
        
            //equations of the form y=c (two horizontal parallel lines)
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance) return false;
        
            double x, y;

            //lineA is vertical x1 = x2
            //slope will be infinity
            //so lets derive another solution
            if (Math.Abs(x1 - x2) < tolerance)
            {
                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x1=c1=x
                //subsitute x=x1 in (4) => -m2x1 + y = c2
                // => y = c2 + m2x1 
                x = x1;
                y = c2 + m2 * x1;
            }
            //lineB is vertical x3 = x4
            //slope will be infinity
            //so lets derive another solution
            else if (Math.Abs(x3 - x4) < tolerance)
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x3=c3=x
                //subsitute x=x3 in (3) => -m1x3 + y = c1
                // => y = c1 + m1x3 
                x = x3;
                y = c1 + m1 * x3;
            }
            //lineA & lineB are not vertical 
            //(could be horizontal we can handle it with slope = 0)
            else
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                //verify by plugging intersection point (x, y)
                //in orginal equations (1) & (2) to see if they intersect
                //otherwise x,y values will not be finite and will fail this check
                if (!(Math.Abs(-m1 * x + y - c1) < tolerance && Math.Abs(-m2 * x + y - c2) < tolerance)) return false;                
            }

            //x,y can intersect outside the line segment since line is infinitely long
            //so finally check if x, y is within both the line segments
            if (IsInsideLine(twoPointsLineA, x, y) && IsInsideLine(twoPointsLineB, x, y))
            {
                intersection = new PointF((float)x, (float)y);
                return true;
            };

            return false;
        }

        private static bool IsInsideLine(PointF[] line, double x, double y)
        {
            return (x >= line[0].X && x <= line[1].X || x >= line[1].X && x <= line[0].X) && (y >= line[0].Y && y <= line[1].Y || y >= line[1].Y && y <= line[0].Y);
        }

        /// <summary>
        ///     Ñóììàðíàÿ äëèíà ëèíèè
        /// </summary>
        /// <param name="line"></param>
        /// <param name="isGeo"></param>
        /// <returns></returns>
        public static double TotalLineDistance(PointF[] line, bool isGeo)
        {
            if (line == null) return 0;
            if (line.Length < 2) return 0;

            double res = 0;
            for(int i=1;i<line.Length;i++)
                if (isGeo)
                    res += GeoUtils.GetLengthMeters(line[i - 1].Y, line[i - 1].X, line[i].Y, line[i].X, false);
                else
                    res += Math.Sqrt((line[i].X - line[i - 1].X) * (line[i].X - line[i - 1].X) + (line[i].Y - line[i - 1].Y) * (line[i].Y - line[i - 1].Y));
            return res;
        }

        /// <summary>
        ///     Ìèíèìàëüíîå ðàññòîÿíèå îò òî÷êè äî ïðÿìîé
        /// </summary>
        /// <param name="pt">òî÷êà</param>
        /// <param name="lineStart">íà÷àëüíàÿ òî÷êà îòðåçêà</param>
        /// <param name="lineEnd">êîíå÷íàÿ òî÷êà îòðåçêà</param>
        /// <param name="pointOnLine">Òî÷êà-ïðîåêöèÿ (ïåðïåíäèêóëÿð) ê ëèíèè</param>
        /// <param name="outside">Òî÷êà-ïðîåêöèÿ íàõîäèòñÿ íà ëèíèè çà ïðåäåëàìè îòðåçêà</param>
        /// <returns></returns>
        public static double DistanceFromPointToLine(PointF pt, PointF lineStart, PointF lineEnd, out PointF pointOnLine, out bool outside, bool isGeo)
        {
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;

            //side = Math.Sign((lineEnd.X - lineStart.X) * (pt.Y - lineStart.Y) - (lineEnd.Y - lineStart.Y) * (pt.X - lineStart.X));

            // Calculate the t that minimizes the distance.
            float t = ((pt.X - lineStart.X) * dx + (pt.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                pointOnLine = new PointF(lineStart.X, lineStart.Y);
                dx = pt.X - lineStart.X;
                dy = pt.Y - lineStart.Y;
                outside = true;
            }
            else if (t > 1)
            {
                pointOnLine = new PointF(lineEnd.X, lineEnd.Y);
                dx = pt.X - lineEnd.X;
                dy = pt.Y - lineEnd.Y;
                outside = true;
            }
            else
            {
                pointOnLine = new PointF(lineStart.X + t * dx, lineStart.Y + t * dy);
                dx = pt.X - pointOnLine.X;
                dy = pt.Y - pointOnLine.Y;
                outside = false;
            };

            if(isGeo)            
                return GeoUtils.GetLengthMeters(pt.Y, pt.X, pointOnLine.Y, pointOnLine.X, false);
            else
                return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        ///     DISTANCE
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <param name="pointOnLine"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        public static double DistanceFromPointToLine(PointF pt, PointF lineStart, PointF lineEnd, out PointF pointOnLine, out int side, bool isGeo)
        {
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;

            side = Math.Sign((lineEnd.X - lineStart.X) * (pt.Y - lineStart.Y) - (lineEnd.Y - lineStart.Y) * (pt.X - lineStart.X));

            // Calculate the t that minimizes the distance.
            float t = ((pt.X - lineStart.X) * dx + (pt.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                pointOnLine = new PointF(lineStart.X, lineStart.Y);
                dx = pt.X - lineStart.X;
                dy = pt.Y - lineStart.Y;
            }
            else if (t > 1)
            {
                pointOnLine = new PointF(lineEnd.X, lineEnd.Y);
                dx = pt.X - lineEnd.X;
                dy = pt.Y - lineEnd.Y;
            }
            else
            {
                pointOnLine = new PointF(lineStart.X + t * dx, lineStart.Y + t * dy);
                dx = pt.X - pointOnLine.X;
                dy = pt.Y - pointOnLine.Y;
            };

            if (isGeo)
                return GeoUtils.GetLengthMeters(pt.Y, pt.X, pointOnLine.Y, pointOnLine.X, false);
            else
                return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class GeoUtils
    {
        // Ðàññ÷åò ðàññòîÿíèÿ       
        #region LENGTH
        public static float GetLengthMeters(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            // use fastest
            float result = (float)GetLengthMetersD(StartLat, StartLong, EndLat, EndLong, radians);

            if (float.IsNaN(result))
            {
                result = (float)GetLengthMetersC(StartLat, StartLong, EndLat, EndLong, radians);
                if (float.IsNaN(result))
                {
                    result = (float)GetLengthMetersE(StartLat, StartLong, EndLat, EndLong, radians);
                    if (float.IsNaN(result))
                        result = 0;
                };
            };

            return result;
        }

        // Slower
        public static uint GetLengthMetersA(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double D2R = Math.PI / 180;     // Ïðåîáðàçîâàíèå ãðàäóñîâ â ðàäèàíû

            double a = 6378137.0000;     // WGS-84 Equatorial Radius (a)
            double f = 1 / 298.257223563;  // WGS-84 Flattening (f)
            double b = (1 - f) * a;      // WGS-84 Polar Radius
            double e2 = (2 - f) * f;      // WGS-84 Êâàäðàò ýêñöåíòðè÷íîñòè ýëëèïñîèäà  // 1-(b/a)^2

            // Ïåðåìåííûå, èñïîëüçóåìûå äëÿ âû÷èñëåíèÿ ñìåùåíèÿ è ðàññòîÿíèÿ
            double fPhimean;                           // Ñðåäíÿÿ øèðîòà
            double fdLambda;                           // Ðàçíèöà ìåæäó äâóìÿ çíà÷åíèÿìè äîëãîòû
            double fdPhi;                           // Ðàçíèöà ìåæäó äâóìÿ çíà÷åíèÿìè øèðîòû
            double fAlpha;                           // Ñìåùåíèå
            double fRho;                           // Ìåðèäèàíñêèé ðàäèóñ êðèâèçíû
            double fNu;                           // Ïîïåðå÷íûé ðàäèóñ êðèâèçíû
            double fR;                           // Ðàäèóñ ñôåðû Çåìëè
            double fz;                           // Óãëîâîå ðàññòîÿíèå îò öåíòðà ñôåðîèäà
            double fTemp;                           // Âðåìåííàÿ ïåðåìåííàÿ, èñïîëüçóþùàÿñÿ â âû÷èñëåíèÿõ

            // Âû÷èñëÿåì ðàçíèöó ìåæäó äâóìÿ äîëãîòàìè è øèðîòàìè è ïîëó÷àåì ñðåäíþþ øèðîòó
            // ïðåäïîëîæèòåëüíî ÷òî ðàññòîÿíèå ìåæäó òî÷êàìè << ðàäèóñà çåìëè
            if (!radians)
            {
                fdLambda = (StartLong - EndLong) * D2R;
                fdPhi = (StartLat - EndLat) * D2R;
                fPhimean = ((StartLat + EndLat) / 2) * D2R;
            }
            else
            {
                fdLambda = StartLong - EndLong;
                fdPhi = StartLat - EndLat;
                fPhimean = (StartLat + EndLat) / 2;
            };

            // Âû÷èñëÿåì ìåðèäèàííûå è ïîïåðå÷íûå ðàäèóñû êðèâèçíû ñðåäíåé øèðîòû
            fTemp = 1 - e2 * (sqr(Math.Sin(fPhimean)));
            fRho = (a * (1 - e2)) / Math.Pow(fTemp, 1.5);
            fNu = a / (Math.Sqrt(1 - e2 * (Math.Sin(fPhimean) * Math.Sin(fPhimean))));

            // Âû÷èñëÿåì óãëîâîå ðàññòîÿíèå
            if (!radians)
            {
                fz = Math.Sqrt(sqr(Math.Sin(fdPhi / 2.0)) + Math.Cos(EndLat * D2R) * Math.Cos(StartLat * D2R) * sqr(Math.Sin(fdLambda / 2.0)));
            }
            else
            {
                fz = Math.Sqrt(sqr(Math.Sin(fdPhi / 2.0)) + Math.Cos(EndLat) * Math.Cos(StartLat) * sqr(Math.Sin(fdLambda / 2.0)));
            };
            fz = 2 * Math.Asin(fz);

            // Âû÷èñëÿåì ñìåùåíèå
            if (!radians)
            {
                fAlpha = Math.Cos(EndLat * D2R) * Math.Sin(fdLambda) * 1 / Math.Sin(fz);
            }
            else
            {
                fAlpha = Math.Cos(EndLat) * Math.Sin(fdLambda) * 1 / Math.Sin(fz);
            };
            fAlpha = Math.Asin(fAlpha);

            // Âû÷èñëÿåì ðàäèóñ Çåìëè
            fR = (fRho * fNu) / (fRho * sqr(Math.Sin(fAlpha)) + fNu * sqr(Math.Cos(fAlpha)));
            // Ïîëó÷àåì ðàññòîÿíèå
            return (uint)Math.Round(Math.Abs(fz * fR));
        }
        // Slowest
        public static uint GetLengthMetersB(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double fPhimean, fdLambda, fdPhi, fAlpha, fRho, fNu, fR, fz, fTemp, Distance,
                D2R = Math.PI / 180,
                a = 6378137.0,
                e2 = 0.006739496742337;
            if (radians) D2R = 1;

            fdLambda = (StartLong - EndLong) * D2R;
            fdPhi = (StartLat - EndLat) * D2R;
            fPhimean = (StartLat + EndLat) / 2.0 * D2R;

            fTemp = 1 - e2 * Math.Pow(Math.Sin(fPhimean), 2);
            fRho = a * (1 - e2) / Math.Pow(fTemp, 1.5);
            fNu = a / Math.Sqrt(1 - e2 * Math.Sin(fPhimean) * Math.Sin(fPhimean));

            fz = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(fdPhi / 2.0), 2) +
              Math.Cos(EndLat * D2R) * Math.Cos(StartLat * D2R) * Math.Pow(Math.Sin(fdLambda / 2.0), 2)));
            fAlpha = Math.Asin(Math.Cos(EndLat * D2R) * Math.Sin(fdLambda) / Math.Sin(fz));
            fR = fRho * fNu / (fRho * Math.Pow(Math.Sin(fAlpha), 2) + fNu * Math.Pow(Math.Cos(fAlpha), 2));
            Distance = fz * fR;

            return (uint)Math.Round(Distance);
        }
        // Average
        public static uint GetLengthMetersC(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double D2R = Math.PI / 180;
            if (radians) D2R = 1;
            double dDistance = Double.MinValue;
            double dLat1InRad = StartLat * D2R;
            double dLong1InRad = StartLong * D2R;
            double dLat2InRad = EndLat * D2R;
            double dLong2InRad = EndLong * D2R;

            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;

            // Intermediate result a.
            double a = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                       Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) *
                       Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            // Intermediate result c (great circle distance in Radians).
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            const double kEarthRadiusKms = 6378137.0000;
            dDistance = kEarthRadiusKms * c;

            return (uint)Math.Round(dDistance);
        }
        // Fastest
        public static double GetLengthMetersD(double sLat, double sLon, double eLat, double eLon, bool radians)
        {
            double EarthRadius = 6378137.0;

            double lon1 = radians ? sLon : DegToRad(sLon);
            double lon2 = radians ? eLon : DegToRad(eLon);
            double lat1 = radians ? sLat : DegToRad(sLat);
            double lat2 = radians ? eLat : DegToRad(eLat);

            return EarthRadius * (Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2)));
        }
        // Fastest
        public static double GetLengthMetersE(double sLat, double sLon, double eLat, double eLon, bool radians)
        {
            double EarthRadius = 6378137.0;

            double lon1 = radians ? sLon : DegToRad(sLon);
            double lon2 = radians ? eLon : DegToRad(eLon);
            double lat1 = radians ? sLat : DegToRad(sLat);
            double lat2 = radians ? eLat : DegToRad(eLat);

            /* This algorithm is called Sinnott's Formula */
            double dlon = (lon2) - (lon1);
            double dlat = (lat2) - (lat1);
            double a = Math.Pow(Math.Sin(dlat / 2), 2.0) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2.0);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return EarthRadius * c;
        }
        
        private static double sqr(double val)
        {
            return val * val;
        }
        public static double DegToRad(double deg)
        {
            return (deg / 180.0 * Math.PI);
        }
        #endregion LENGTH
    }    
}
