﻿using Epico.Sistema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epico
{
    public abstract class Eixos2 : Eixos
    {
        public Eixos2() => Dim = new float[2];
        public Eixos2(float X, float Y) => Dim = new float[2] { X, Y };
        public Eixos2(Eixos2 eixos)
        {
            Obj = eixos.Obj;
            Dim = eixos.Dim;
            Nome = eixos.Nome;
            Tag = eixos.Tag;
        }
        public Eixos2(Geometria Obj)
        {
            this.Obj = Obj;
            Dim = new float[2];
        }
        public Eixos2(Geometria Obj, Eixos2 eixos)
        {
            base.Obj = Obj;
            Dim = new float[2] { eixos.Dim[0], eixos.Dim[1] };
        }
        public Eixos2(Geometria Obj, float X, float Y) {
            base.Obj = Obj;
            Dim = new float[2] { X, Y };
        }

        /// <summary>Coordenada X</summary>
        public float X { get => Dim[0]; set => Dim[0] = value; }
        /// <summary>Coordenada Y</summary>
        public float Y { get => Dim[1]; set => Dim[1] = value; }

        public Eixos2 Global { get => Obj.Pos + this; }

        public static Eixos2 operator +(Eixos2 a, Vertice2 b)
        {
            Vertice2 ret = new Vertice2(b.Obj);
            ret.X = a.X + b.X;
            ret.Y = a.Y + a.Y;
            return ret;
        }

        public static Eixos2 operator +(Eixos2 a, Eixos2 b)
        {
            Eixos2 ret = (Eixos2)a.NovaInstancia();
            ret.X = a.X + b.X;
            ret.Y = a.Y + b.Y;
            return ret;
        }

        public static Eixos2 operator -(Eixos2 a, Eixos2 b)
        {
            Vetor2 ret = new Vetor2();
            ret.X = a.X - b.X;
            ret.Y = a.Y - b.Y;
            return ret;
        }
    }
}
