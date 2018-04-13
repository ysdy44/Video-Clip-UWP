using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 剪片.Library
{
    class Design_Patterns
    {
        //1.单例模式

        //反例：高并发时会产生多个对象
        public class Singleton
        {
            private static Singleton singleton = null;
            //通过该方法获得实例对象
            public static Singleton getSingleton()
            {
                if (singleton == null) singleton = new Singleton();
                return singleton;
            }
        }

        //正例：不会产生多个对象
        public class Singleton2
        {
            private static Singleton2 singleton = new Singleton2();
            //通过该方法获得实例对象
            public static Singleton2 getSingleton()
            {
                return singleton;
            }
        }


        //2.工厂方法模式


        //产品
        public interface Human
        {
            void GetColor();
         
                void Talk();
        }
        public class WhiteHuman : Human
        {
            public void GetColor()
            {
                string s = "白色";
            }
            public void Talk()
            {
                string s = "我是白人";
            }
        }
        public class BlackHuman : Human
        {
            public void GetColor()
            {
                string s = "黑色";
            }
            public void Talk()
            {
                string s = "我是黑人";
            }
        }
        public class YelllowHuman : Human
        {
            public void GetColor()
            {
                string s = "黄色";
            }
            public void Talk()
            {
                string s = "我是黄人";
            }
        }


        //工厂
        public abstract class HumanFactory
        {
            public abstract Human createHUman();
        }

        public class WhiteHumanFactory : HumanFactory
        {
            public override Human createHUman()
            {
                return new WhiteHuman();
            }
        }
        public class BlackHumanFactory : HumanFactory
        {
            public override Human createHUman()
            {
                return new BlackHuman();
            }
        }
        public class YelllowHumanFactory : HumanFactory
        {
            public override Human createHUman()
            {
                return new YelllowHuman();
            }
        }

        //使用
        public class HumanUse
        {
            Human wm = (new WhiteHumanFactory()).createHUman(); //工厂生产白人
    //     var m= wm.ToString();


            Human bm = (new BlackHumanFactory()).createHUman(); //工厂生产黑人
            
            Human ym = (new YelllowHumanFactory()).createHUman();//工厂生产黄人
        }





    }
}
