﻿using EpicoGraphics.Sistema2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicoGraphics.Controles
{

    public class Button2D : Controle2D
    {
        private readonly string _nomePadrao = "Button";

        public Button2D(EpicoGraphics engine, Controle2D parent)
        {
            _epico = engine;

            Nome = _nomePadrao;
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));

            XY proxPos = ProximoPosControle();
            GerarControle(proxPos.X, proxPos.Y, 100, 100);
            Mat_render.CorSolida = new RGBA(200, 0, 200, 88);
        }
    }
}