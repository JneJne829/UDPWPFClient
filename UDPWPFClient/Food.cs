
namespace FoodNamespace
{
    public class Food
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Diameter { get; set; }

        public int Color { get; set; }

        //public Ellipse Ellipse { get; set; } // Ellipse 對象的引用
        public int Key { get; set; }


        public Food(double x, double y, int col, int row, double diameter, int color, int key)
        {
            X = x;
            Y = y;
            Col = col;
            Row = row;
            Diameter = diameter;
            Color = color;
            Key = key;
            /*Ellipse = new Ellipse // 創建 Ellipse 對象
            {
                Width = diameter,
                Height = diameter,
            };*/
        }
    }


}
