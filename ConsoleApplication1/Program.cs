using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            C c = new C();
            Console.WriteLine("Object Created ");
            Console.WriteLine("Press enter to Destroy it");
            Console.ReadLine();
            Console.Read();
        }
    }
    class A : IDisposable
    {
        bool isend = false;
        public A()
        {
            Console.WriteLine("Creating A");
            Thread thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }

        private void Run()
        {
            while (true)
            {
                if (isend)
                    break;
                Thread.Sleep(1000);
            }
        }
        ~A()
        {
            Console.WriteLine("Destroying A");
        }

        public void Dispose()
        {
            isend = true;
        }
    }

    class B : A
    {
        public B()
        {
            Console.WriteLine("Creating B");
        }
        ~B()
        {
            Console.WriteLine("Destroying B");
        }

    }
    class C : B
    {
        public C()
        {
            Console.WriteLine("Creating C");
        }

        ~C()
        {
            Console.WriteLine("Destroying C");
        }
    }
}
