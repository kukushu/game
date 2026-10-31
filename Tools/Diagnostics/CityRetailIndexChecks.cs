namespace HarborCity.RetailChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static class CityRetailIndexChecks
    {
        static int checks;
        static void Require(bool value){checks++;if(!value)throw new Exception("Retail index differs from frozen implementation or independent oracle");}
        static void Main()
        {
            var random=new Random(7919);var current=new CityRetailIndex();var frozen=new CityRetailIndexFrozen();int next=1;
            // Reuse the same indices across empty, sparse and dense periods, so
            // stale buckets, clearing and reactivation are tested as well.
            for(int period=0;period<100;period++)
            {
                current.Begin();frozen.Begin();Require(current.Count==0&&frozen.Count==0);
                var inserted=new List<(CommercialBuilding shop,int bx,int bz)>();
                int count=new[]{0,1,2,12,400}[period%5];
                for(int i=0;i<count;i++)
                {
                    var shop=new CommercialBuilding{id=next++,x=(float)(random.NextDouble()*1600-800),z=(float)(random.NextDouble()*1600-800)};
                    if(i<4){shop.x=(i%2==0?-1:1)*16;shop.z=i<2?-16:16;}
                    current.Add(shop);frozen.Add(shop);inserted.Add((shop,(int)Math.Floor(shop.x/16),(int)Math.Floor(shop.z/16)));
                }
                Require(current.Count==count&&frozen.Count==count);
                // The old index reads live geometry but retains the insertion
                // bucket until Begin. Preserve that behavior too.
                if(period%7==0&&count>0)inserted[0].shop.x+=32;
                for(int query=0;query<32;query++)
                {
                    float x=query<8?0:(float)(random.NextDouble()*2400-1200),z=query<8?0:(float)(random.NextDouble()*2400-1200);
                    float radius=new float[]{0,1,16,31,64,128,512,2048}[query%8];
                    var actual=current.Collect(x,z,radius);var old=frozen.Collect(x,z,radius);
                    int ax=(int)Math.Floor((x-radius)/16),bx=(int)Math.Floor((x+radius)/16),az=(int)Math.Floor((z-radius)/16),bz=(int)Math.Floor((z+radius)/16);
                    double limit=(double)radius*radius;
                    var expected=inserted.Where(v=>v.bx>=ax&&v.bx<=bx&&v.bz>=az&&v.bz<=bz).Select(v=>{double dx=v.shop.x-x,dz=v.shop.z-z;return new{v.shop,squared=dx*dx+dz*dz};}).Where(v=>v.squared<=limit).OrderBy(v=>v.squared).ThenBy(v=>v.shop.id).ToArray();
                    Require(actual.Count==old.Count&&actual.Count==expected.Length);
                    for(int i=0;i<actual.Count;i++)
                    {
                        Require(ReferenceEquals(actual[i].shop,old[i].shop)&&ReferenceEquals(actual[i].shop,expected[i].shop));
                        Require(BitConverter.DoubleToInt64Bits(actual[i].squared)==BitConverter.DoubleToInt64Bits(old[i].squared)&&BitConverter.DoubleToInt64Bits(actual[i].squared)==BitConverter.DoubleToInt64Bits(expected[i].squared));
                    }
                }
            }
            Console.WriteLine("PASS "+checks+" retail index checks: frozen and independent ordered geometry oracle; 100 reused sparse/dense/empty periods, negative coordinates, bucket boundaries, equal distances and live geometry.");
        }
    }
}
