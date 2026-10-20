using System;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Transient geometry only. The existing road transaction owns topology and payment.
    public sealed class CityRoadGrid
    {
        public const int MaxCells=32;
        public readonly Point[][] Strokes;
        public readonly int Columns,Rows;
        public CityRoadGrid(Point start,Point end,int spacing,float bound,Func<float,float,float> height,bool alternatingOneWay=false)
        {
            if(spacing<24||spacing>64||height==null)throw new ArgumentException("Invalid grid settings");
            if(Math.Abs(start.x)>bound||Math.Abs(start.z)>bound)throw new CityRuleException("ui.grid.outside");
            double dx=Math.Max(-bound,Math.Min(bound,end.x))-start.x,dz=Math.Max(-bound,Math.Min(bound,end.z))-start.z;
            Columns=Math.Min(MaxCells,(int)(Math.Abs(dx)/spacing));Rows=Math.Min(MaxCells,(int)(Math.Abs(dz)/spacing));
            if(Columns==0||Rows==0)throw new CityRuleException("ui.grid.too_small",spacing);
            int sx=dx<0?-1:1,sz=dz<0?-1:1;
            Strokes=new Point[Rows+Columns+2][];
            for(int row=0;row<=Rows;row++)Strokes[row]=Stroke(start.x,start.z+sz*row*spacing,start.x+sx*Columns*spacing,start.z+sz*row*spacing,height,alternatingOneWay&&row%2==1);
            for(int col=0;col<=Columns;col++)Strokes[Rows+1+col]=Stroke(start.x+sx*col*spacing,start.z,start.x+sx*col*spacing,start.z+sz*Rows*spacing,height,alternatingOneWay&&col%2==0);
        }
        static Point[] Stroke(double x,double z,double ex,double ez,Func<float,float,float> height,bool reverse)
        {
            int count=(int)Math.Ceiling(Math.Max(Math.Abs(ex-x),Math.Abs(ez-z))/4)+1;var points=new Point[count];
            for(int i=0;i<count;i++){double t=i/(double)(count-1),px=x+(ex-x)*t,pz=z+(ez-z)*t;float y=height((float)px,(float)pz);if(float.IsNaN(y)||float.IsInfinity(y)||y<=.15f)throw new CityRuleException("ui.build.water");points[i]=new Point(px,y,pz);}
            if(reverse)Array.Reverse(points);return points;
        }
    }
}
