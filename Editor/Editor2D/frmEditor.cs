﻿using Epico;
using Epico.Luzes;
using Epico.Objetos2D.Avancados;
using Epico.Objetos2D.Primitivos;
using Epico.Sistema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Editor2D
{
    public partial class frmEditor : Form
    {
        bool _sair = false;
        const int _raio_padrao = 50;

        Epico2D  _engine2D = new Epico2D();
        List<Objeto2D> _objs_sel = new List<Objeto2D>();
        List<Origem2D> _origens_sel = new List<Origem2D>();
        List<Vertice2D> _vetores_sel = new List<Vertice2D>();
        List<Vertice2D> _vertices_sel = new List<Vertice2D>();

        List<ToolStripButton> _ferramentasTransformacao = new List<ToolStripButton>();
        List<ToolStripButton> _ferramentasSelecao = new List<ToolStripButton>();

        bool moveCamera = false;

        #region Retângulo da Ferramenta Multi-Seleção
        private PointF selStart;
        private const byte selAlpha = 70;
        private RectangleF selRect = new RectangleF();
        private Brush selBrush = new SolidBrush(Color.FromArgb(selAlpha, 72, 145, 220));
        #endregion

        public frmEditor()
        {
            InitializeComponent();

            _ferramentasSelecao.Add(toolStripSelecao);
            _ferramentasSelecao.Add(toolStripOrigem);
            _ferramentasSelecao.Add(toolStripVetor);
            _ferramentasSelecao.Add(toolStripVertice);

            _ferramentasTransformacao.Add(toolStripMove);
            _ferramentasTransformacao.Add(toolStripAngulo);
            _ferramentasTransformacao.Add(toolStripRaio);
            _ferramentasTransformacao.Add(toolStripEscala);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Cria a Câmera
            _engine2D.CriarCamera(picScreen.ClientRectangle.Width, picScreen.ClientRectangle.Height);
            #endregion

            #region Define os atributos dos controles

            HabilitarFerramentasTransformacao(false);

            DefineMaxMinValues(
                txtCamPosX, txtCamPosY, txtCamAngulo, txtCamZoom,
                txtPosX, txtPosY, txtRaio, txtAngulo, txtEscalaX, txtEscalaY,
                txtOrigemPosX, txtOrigemPosY,
                txtVerticePosX, txtVerticePosY, txtVerticeRaio, txtVerticeAngulo);

            BtnCirculo_Click(sender, e);
            AtualizarControlesObjeto2D(_objs_sel);
            AtualizarComboObjetos2D();

            debugToolStripMenuItem.Checked = _engine2D.Debug = true;
            desligarZoomToolStripMenuItem.Checked = _engine2D.Camera.DesligarSistemaZoom = true;

            cboCamera.DisplayMember = "Nome";
            cboCamera.ValueMember = "Cam";
            cboCamera.DataSource = _engine2D.Cameras.Select(
                Cam => new
                {
                    Cam.Id,
                    Cam.Nome,
                    Cam
                }).ToList();
            #endregion

            Show();

            #region  Loop principal de rotinas do simulador 2D
            while (!_sair)
            {
                // Use o tempo delta em todos os cálculos que alteram o comportamento dos objetos 2d
                // para que rode em processadores de baixo e alto desempenho sem alterar a qualidade do simulador

                // TODO: Insira toda sua rotina aqui

                if (moveCamera)
                {
                    EixoXY xyCamDrag = new XY(cameraDrag.X, cameraDrag.Y);
                    EixoXY xyCursor = new XY(Cursor.Position.X, Cursor.Position.Y);
                    float distCursor = Util.DistanciaEntreDoisPontos(xyCamDrag, xyCursor);
                    float angCursor = Util.AnguloEntreDoisPontos(xyCamDrag, xyCursor);

                    _engine2D.Camera.Pos.X += (float)(Math.Cos(Util.Angulo2Radiano(angCursor)) * distCursor * _engine2D.Camera.TempoDelta * 0.000001);
                    _engine2D.Camera.Pos.Y += (float)(Math.Sin(Util.Angulo2Radiano(angCursor)) * distCursor * _engine2D.Camera.TempoDelta * 0.000001);
                }

                if (_engine2D.Camera.ResWidth != picScreen.ClientRectangle.Width ||
                    _engine2D.Camera.ResHeigth != picScreen.ClientRectangle.Height)
                {
                    _engine2D.Camera.RedefinirResolucao(picScreen.ClientRectangle.Width, picScreen.ClientRectangle.Height);
                }

                picScreen.Image = _engine2D.Camera.Renderizar();
                Application.DoEvents();
            }
            #endregion
        }

        private void DefineMaxMinValues(params NumericUpDown[] numericUpDown)
        {
            numericUpDown.ToList().ForEach(x => 
            {
                x.Maximum = decimal.MaxValue;
                x.Minimum = decimal.MinValue;
            });
        }

        private void AtualizarControlesObjeto2D(List<Objeto2D> selecionados)
        {
            // Nenhum objeto selecionado?
            if (selecionados.Count == 0)
            {
                // Desabilita todas as ferramentas de transformação
                HabilitarFerramentasTransformacao(false);

                txtVisivel.Text = string.Empty;
                txtPosX.Text = string.Empty;
                txtPosY.Text = string.Empty;
                txtAngulo.Text = string.Empty;
                txtRaio.Text = string.Empty;
                txtEscalaX.Text = string.Empty;
                txtEscalaY.Text = string.Empty;

                cboVertices.DataSource = null;
                txtVerticeAngulo.Text = string.Empty;
                txtVerticePosX.Text = string.Empty;
                txtVerticePosY.Text = string.Empty;
                txtVerticeRaio.Text = string.Empty;
            }
            else
            {
                // Reabilita todas as ferramentas de transformação
                HabilitarFerramentasTransformacao(true);

                if (selecionados.Count == 1) // Único objeto selecionado?
                {
                    txtPosX.Text = selecionados.First().Pos.X.ToString();
                    txtPosY.Text = selecionados.First().Pos.Y.ToString();
                    txtAngulo.Text = selecionados.First().Angulo.ToString();
                    txtRaio.Text = selecionados.First().Raio.ToString();
                    txtEscalaX.Text = selecionados.First().Escala.X.ToString();
                    txtEscalaY.Text = selecionados.First().Escala.Y.ToString();
                }
                else // Muitos objetos selecionados?
                {
                    txtVisivel.Text = string.Empty;
                    txtPosX.Text = string.Empty;
                    txtPosY.Text = string.Empty;
                    txtAngulo.Text = string.Empty;
                    txtRaio.Text = string.Empty;
                    txtEscalaX.Text = string.Empty;
                    txtEscalaY.Text = string.Empty;

                    cboVertices.DataSource = null;
                    txtVerticeAngulo.Text = string.Empty;
                    txtVerticePosX.Text = string.Empty;
                    txtVerticePosY.Text = string.Empty;
                    txtVerticeRaio.Text = string.Empty;
                }
            }
        }

        PointF cameraDrag;
        private void PicDesign_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                cameraDrag = Cursor.Position;
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (toolStripSelecao.Checked || toolStripVertice.Checked ||
                    toolStripOrigem.Checked || toolStripVetor.Checked)
                {
                    selStart = e.Location;
                }
            }
        }

        private Vetor2D PosAleatorio()
        {
            int x = new Random(Environment.TickCount).Next(0, picScreen.ClientRectangle.Width);
            int y = new Random(Environment.TickCount + x).Next(0, picScreen.ClientRectangle.Height);

            _engine2D.Camera.Pos = new Vetor2D(x, y);
            return new Vetor2D(x, y);
        }

        private void AtualizarComboObjetos2D()
        {
            cboObjeto2D.BeginUpdate();
            cboObjeto2D.DisplayMember = "Nome";
            cboObjeto2D.ValueMember = "o";
            cboObjeto2D.DataSource = _engine2D.objetos
                .Select(o => new
                {
                    o.Id,
                    o.Nome,
                    o
                }).ToList();
            cboObjeto2D.EndUpdate();
        }

        private void BtnQuadrado_Click(object sender, EventArgs e)
        {
            Quadrado obj = new Quadrado();
            obj.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            obj.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            obj.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            obj.GerarGeometria(45, _raio_padrao);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }


        private void BtnCirculo_Click(object sender, EventArgs e)
        {
            Circulo obj = new Circulo();
            obj.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            obj.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            obj.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            obj.GerarGeometria(0, _raio_padrao);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }

        private void BtnTriangulo_Click(object sender, EventArgs e)
        {
            Triangulo obj = new Triangulo();
            obj.Pos = PosAleatorio();

            var rnd = new Random(Environment.TickCount);
            obj.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            obj.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            obj.GerarGeometria(0, _raio_padrao);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }

        private void BtnPentagono_Click(object sender, EventArgs e)
        {
            Pentagono obj = new Pentagono();
            obj.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            obj.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            obj.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            obj.GerarGeometria(0, _raio_padrao);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }

        private void BtnHexagono_Click(object sender, EventArgs e)
        {
            Hexagono hexagono = new Hexagono();
            hexagono.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            hexagono.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            hexagono.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            hexagono.GerarGeometria(0, _raio_padrao);
            _engine2D.AddObjeto(hexagono);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = hexagono;

        }

        private void BtnLosango_Click(object sender, EventArgs e)
        {
            Losango losango = new Losango();
            losango.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            losango.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            losango.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            losango.GerarGeometria(0, _raio_padrao);
            _engine2D.AddObjeto(losango);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = losango;
        }

        private void BtnTrianguloRetangulo_Click(object sender, EventArgs e)
        {
            TrianguloRetangulo triangulo = new TrianguloRetangulo();
            triangulo.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            triangulo.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            triangulo.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            triangulo.GerarGeometria(45, _raio_padrao);
            _engine2D.AddObjeto(triangulo);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = triangulo;
        }

        private void BtnRetangulo_Click(object sender, EventArgs e)
        {
            Retangulo retangulo = new Retangulo();
            retangulo.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            retangulo.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            retangulo.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            retangulo.GerarGeometria(45, _raio_padrao);
            _engine2D.AddObjeto(retangulo);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = retangulo;
        }

        private void TxtNome_TextChanged(object sender, EventArgs e)
        {
            if (_objs_sel.Count == 1 && cboObjeto2D.Focused)
            {
                _objs_sel.First().Nome = cboObjeto2D.Text;
            }
        }

        private void TxtAngulo_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtAngulo.Focused)
            {
                if (float.TryParse(txtAngulo.Text, out float angulo))
                {
                    _objs_sel.First().DefinirAngulo(angulo);
                }
            }
        }

        private void TxtRaio_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtRaio.Focused)
            {
                if (float.TryParse(txtRaio.Text, out float raio))
                {
                    _objs_sel.First().DefinirRaio(raio);
                }
            }
        }

        private void TxtPosY_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtPosY.Focused)
            {
                if (float.TryParse(txtPosY.Text, out float posY))
                {
                    _objs_sel.First().PosicionarY(posY);
                }
            }
        }

        private void TxtPosX_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtPosX.Focused)
            {
                if (float.TryParse(txtPosX.Text, out float posX))
                {
                    _objs_sel.First().PosicionarX(posX);
                }
            }
        }

        private void PicDesign_MouseMove(object sender, MouseEventArgs e)
        {
            moveCamera = false;
            if (e.Button == MouseButtons.Left)
            {
                if (toolStripSelecao.Checked || toolStripVertice.Checked ||
                    toolStripOrigem.Checked || toolStripVetor.Checked)
                {
                    // Retângulo Multi-Seleção
                    PointF tempEndPoint = e.Location;
                    selRect.Location = new PointF(
                        Math.Min(selStart.X, tempEndPoint.X),
                        Math.Min(selStart.Y, tempEndPoint.Y));
                    selRect.Size = new SizeF(
                        Math.Abs(selStart.X - tempEndPoint.X),
                        Math.Abs(selStart.Y - tempEndPoint.Y));
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                moveCamera = true;
            }
        }

        private void TxtCamPosX_ValueChanged(object sender, EventArgs e)
        {
            if (txtCamPosX.Focused)
            {
                if (float.TryParse(txtCamPosX.Text, out float camPosX))
                {
                    _engine2D.Camera.Pos.X = camPosX;
                }
            }
        }

        private void TxtCamPosY_ValueChanged(object sender, EventArgs e)
        {
            if (txtCamPosY.Focused)
            {
                if (float.TryParse(txtCamPosY.Text, out float camPosY))
                {
                    _engine2D.Camera.Pos.Y = camPosY;
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            
        }

        private void TxtEscalaY_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtEscalaY.Focused)
            {
                if (float.TryParse(txtEscalaY.Text, out float escalaY))
                {
                    _objs_sel.First().DefinirEscalaY(escalaY);
                }
            }
        }
        private void TxtEscalaX_ValueChanged(object sender, EventArgs e)
        {
            if (_objs_sel != null && txtEscalaX.Focused)
            {
                if (float.TryParse(txtEscalaX.Text, out float escalaX))
                {
                    _objs_sel.First().DefinirEscalaX(escalaX);
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _sair = true;
        }

        private void TmrObjeto2D_Tick(object sender, EventArgs e)
        {
            if (_sair) return;
            
            if (_objs_sel.Count == 1)
            {
                if (_engine2D.Camera.Objeto2DVisivelCamera(_objs_sel.First()))
                    txtVisivel.Text = "Sim";
                else
                    txtVisivel.Text = "Não";
            }

            if (!txtCamPosX.Focused) txtCamPosX.Value = (decimal)_engine2D.Camera.Pos.X;
            if (!txtCamPosY.Focused) txtCamPosY.Value = (decimal)_engine2D.Camera.Pos.Y;
            if (!txtCamAngulo.Focused) txtCamAngulo.Value = (decimal)_engine2D.Camera.Angulo;
            if (!txtCamZoom.Focused) txtCamZoom.Value = (decimal)_engine2D.Camera.ZoomCamera;
        }

        private void BtnVarios_Click(object sender, EventArgs e)
        {
            int quant = 50 / 4;

            for (int i = 0; i < quant; i++)
            {
                BtnCirculo_Click(sender, e);
                Thread.Sleep(20);
                BtnTriangulo_Click(sender, e);
                Thread.Sleep(20);
                BtnQuadrado_Click(sender, e);
                Thread.Sleep(20);
                BtnPentagono_Click(sender, e);
                Thread.Sleep(20);
            }
        }

        private void BtnFocarObjeto_Click(object sender, EventArgs e)
        {
            if (cboObjeto2D.SelectedValue != null)
            {
                _engine2D.Camera.Focar((Objeto2D)cboObjeto2D.SelectedValue);
            }
        }

        private void CboObjeto2D_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjeto2D.SelectedValue != null)
            {
                _objs_sel.ForEach(x => x.Selecionado = false);
                _objs_sel.Clear();
                _objs_sel.Add((Objeto2D)cboObjeto2D.SelectedValue);
                _objs_sel.First().Selecionado = true;
                AtualizarControlesObjeto2D(_objs_sel);

                //_vertices_sel.ForEach(x => x.sel = false);
                //_vertices_sel.Clear();

                LimparSelecoesGeometricas();

                #region Vértices
                cboVertices.BeginUpdate();
                cboVertices.DisplayMember = "i";
                cboVertices.ValueMember = "v";
                cboVertices.DataSource = _objs_sel.First().Vertices.Select(
                (v, i) => new
                {
                    i = "Vértice " + i,
                    v
                }).ToList();
                cboVertices.EndUpdate();
                #endregion

                #region Pontos Centrais
                cboOrigem.BeginUpdate();
                cboOrigem.DisplayMember = "i";
                cboOrigem.ValueMember = "c";
                cboOrigem.DataSource = _objs_sel.First().Origem.Select(
                (c, i) => new
                {
                    i = "Ponto " + i,
                    c
                }).ToList();
                cboOrigem.EndUpdate();
                #endregion
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (pnScreen.Focused)
            {
                if (_objs_sel != null)
                {
                    if (e.KeyCode == Keys.Delete)
                    {
                        for (int i = 0; i < _objs_sel.Count; i++)
                        {
                            _engine2D.objetos.Remove(_objs_sel[i]);
                        }
                        
                        _objs_sel.Clear();
                        AtualizarControlesObjeto2D(_objs_sel);
                    }
                }
            }
        }

        [DllImport("user32")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        // you also need ReleaseDC
        [DllImport("user32")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
        private void TelaCheiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //IntPtr hdc = GetWindowDC(this.Handle);
            //Graphics g = Graphics.FromHdc(hdc);

            //engine2D.Camera.g = g;
        }

        private void FPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fPSToolStripMenuItem.Checked = !fPSToolStripMenuItem.Checked;
            _engine2D.Debug = fPSToolStripMenuItem.Checked;
        }

        private void BtnNovaCamera_Click(object sender, EventArgs e)
        {
            #region Cria a Câmera 2D
            Camera2D camera = _engine2D.CriarCamera(picScreen.Width, picScreen.Height);
            camera.Pos = new Vetor2D(_objs_sel.First().Pos.X, _objs_sel.First().Pos.Y);
            #endregion

            cboCamera.DataSource = _engine2D.Cameras
                .Select(cam => new
                {
                    cam.Id,
                    cam.Nome,
                    cam
                }).ToList();

            cboCamera.SelectedValue = camera;
        }

        private void CboCamera_SelectedValueChanged(object sender, EventArgs e)
        {
            _engine2D.Camera = (Camera2D)cboCamera.SelectedValue;
        }

        private void BtnQuadrilatero_Click(object sender, EventArgs e)
        {
            Quadrilatero quadrilatero = new Quadrilatero();
            quadrilatero.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            quadrilatero.Mat_render.CorBorda = new RGBA(255, (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            quadrilatero.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            quadrilatero.GerarGeometria(rnd.Next(0, 359), _raio_padrao, (int)(_raio_padrao * 1.5F));
            _engine2D.AddObjeto(quadrilatero);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = quadrilatero;
        }

        private void BtnEstrela_Click(object sender, EventArgs e)
        {
            Estrela estrela = new Estrela();
            estrela.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            estrela.Mat_render.CorBorda = new RGBA(255, (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.GerarGeometria(rnd.Next(0, 359), _raio_padrao, (int)(_raio_padrao * 1.5F), 10);
            _engine2D.AddObjeto(estrela);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = estrela;

        }


        private void BtnEstrelaQuaPon_Click(object sender, EventArgs e)
        {
            Estrela estrela = new Estrela();
            estrela.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            estrela.Mat_render.CorBorda = new RGBA(255, (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.GerarGeometria(rnd.Next(0, 359), _raio_padrao, (int)(_raio_padrao * 1.5F),8);
            _engine2D.AddObjeto(estrela);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = estrela;
        }

        private void BtnEstrelaSeisPontas_Click(object sender, EventArgs e)
        {
            Estrela estrela = new Estrela();
            estrela.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            estrela.Mat_render.CorBorda = new RGBA(255, (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            estrela.GerarGeometria(rnd.Next(0, 359), _raio_padrao, (int)(_raio_padrao * 1.5F),12);
            _engine2D.AddObjeto(estrela);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = estrela;
        }

        private void PicScreen_Paint(object sender, PaintEventArgs e)
        {
            if (picScreen.Image != null)
            {
                if (selRect.Width > 0 && selRect.Height > 0) // Retângulo
                {
                    // Desenha o retângulo multi-seleção
                    e.Graphics.FillRectangle(selBrush, selRect);

                    if (toolStripSelecao.Checked)
                    {
                        _objs_sel.ForEach(x => x.Selecionado = false);
                        _objs_sel.Clear();
                        _objs_sel = _engine2D.ObterObjetos2DPelaTela(_engine2D.Camera, selRect).ToList();
                        _objs_sel.ForEach(x => x.Selecionado = true);

                        // Informa a quantidade de objetos presentes na área do retângulo
                        var tmp = Util.ObterObjetos2DPelaTela(_engine2D, _engine2D.Camera, selRect);
                        e.Graphics.DrawString(
                            $"{tmp.Count()} objetos", new Font("Lucida Console", 10),
                            new SolidBrush(Color.FromArgb(selAlpha, 255, 255, 255)),
                            new RectangleF(selRect.Location, selRect.Size),
                            new StringFormat(StringFormatFlags.NoWrap));
                    }
                    else if (toolStripOrigem.Checked)
                    {
                        // Informa a quantidade de objetos presentes na área do retângulo
                        _origens_sel.ForEach(x => x.Sel = false);
                        _origens_sel.Clear();
                        _origens_sel = Util.ObterOrigensObjeto2DPelaTela(_engine2D.Camera, _objs_sel, selRect).ToList();
                        _origens_sel.ForEach(x => x.Sel = true);

                        e.Graphics.DrawString(
                            $"{_origens_sel.Count()} origens", new Font("Lucida Console", 10),
                            new SolidBrush(Color.FromArgb(selAlpha, 255, 255, 255)),
                            new RectangleF(selRect.Location, selRect.Size),
                            new StringFormat(StringFormatFlags.NoWrap));
                    }
                    else if (toolStripVetor.Checked)
                    {
                        // Informa a quantidade de objetos presentes na área do retângulo
                        _vetores_sel.ForEach(x => x.Sel = false);
                        _vetores_sel.Clear();
                        _vetores_sel = Util.ObterVetoresObjeto2DPelaTela(_engine2D.Camera, _objs_sel, selRect).ToList();
                        _vetores_sel.ForEach(x => x.Sel = true);

                        e.Graphics.DrawString(
                            $"{_vetores_sel.Count()} vetores", new Font("Lucida Console", 10),
                            new SolidBrush(Color.FromArgb(selAlpha, 255, 255, 255)),
                            new RectangleF(selRect.Location, selRect.Size),
                            new StringFormat(StringFormatFlags.NoWrap));
                    }
                    else if (toolStripVertice.Checked)
                    {
                        // Informa a quantidade de objetos presentes na área do retângulo
                        _vertices_sel.ForEach(x => x.Sel = false);
                        _vertices_sel.Clear();
                        _vertices_sel = Util.ObterVerticesObjeto2DPelaTela(_engine2D.Camera, _objs_sel, selRect).ToList();
                        _vertices_sel.ForEach(x => x.Sel = true);

                        e.Graphics.DrawString(
                            $"{_vertices_sel.Count()} vértices", new Font("Lucida Console", 10),
                            new SolidBrush(Color.FromArgb(selAlpha, 255, 255, 255)),
                            new RectangleF(selRect.Location, selRect.Size),
                            new StringFormat(StringFormatFlags.NoWrap));
                    }
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
        }

        private void TxtCamZoom_ValueChanged(object sender, EventArgs e)
        {
            if (txtCamZoom.Focused)
            {
                if (float.TryParse(txtCamZoom.Text, out float camZoom))
                {
                    _engine2D.Camera.DefinirZoom(camZoom);
                }
            }
        }

        private void DesligarZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            desligarZoomToolStripMenuItem.Checked = !desligarZoomToolStripMenuItem.Checked;
            _engine2D.Camera.DesligarSistemaZoom = desligarZoomToolStripMenuItem.Checked;
        }

        private void BtnDeformado_Click(object sender, EventArgs e)
        {
            Deformado obj = new Deformado();
            obj.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            obj.Mat_render.CorBorda = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
            obj.Mat_render.CorSolida = new RGBA((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));

            obj.GerarGeometria(0, 5, 50);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }

        private void BtnLuzPonto_Click(object sender, EventArgs e)
        {
            LuzPonto obj = new LuzPonto(150, 150);
            obj.Pos = PosAleatorio();
            var rnd = new Random(Environment.TickCount);
            _engine2D.AddObjeto(obj);

            AtualizarComboObjetos2D();
            cboObjeto2D.SelectedValue = obj;
        }

        private void BtnLuzDirecional_Click(object sender, EventArgs e)
        {
            // TODO: Luz Ambiente
            throw new NotImplementedException();
        }

        private void BtnLuzDestaque_Click(object sender, EventArgs e)
        {
            // TODO: Luz Lanterna
            throw new NotImplementedException();
        }

        private void CboVertices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboVertices.SelectedValue != null)
            {
                _vertices_sel.ForEach(x => x.Sel = false);
                _vertices_sel.Clear();
                _vertices_sel.Add((Vertice2D)cboVertices.SelectedValue);
                if (toolStripVertice.Checked) _vertices_sel.First().Sel = true;
                AtualizarControlesVertice(_vertices_sel);
            }
        }

        private void AtualizarControlesVertice(List<Vertice2D> vertices)
        {
            if (vertices.Count == 1)
            {
                txtVerticePosX.Text = vertices.First().X.ToString();
                txtVerticePosY.Text = vertices.First().Y.ToString();
                txtVerticeAngulo.Text = vertices.First().Ang.ToString();
                txtVerticeRaio.Text = vertices.First().Raio.ToString();
            }
            else
            {
                txtVerticePosX.Text = string.Empty;
                txtVerticePosY.Text = string.Empty;
                txtVerticeAngulo.Text = string.Empty;
                txtVerticeRaio.Text = string.Empty;
            }
        }

        private void PicScreen_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (toolStripSelecao.Checked) // Ferramenta Seleção de Objetos
                {
                    if (selRect.Width == 0 && selRect.Height == 0)
                    {
                        _vertices_sel.ForEach(x => x.Sel = false);
                        _vertices_sel.Clear();
                        _objs_sel.ForEach(x => x.Selecionado = false);
                        _objs_sel = Util.ObterObjetos2DPelaTela(_engine2D, _engine2D.Camera, selStart).ToList();
                    }

                    if (_objs_sel.Count() == 0)
                    {
                        // Nenhum objeto selecionado
                        cboObjeto2D.SelectedIndex = -1;
                        
                    }
                    else if (_objs_sel.Count == 1)
                    {
                        // Único objeto selecionado
                        cboObjeto2D.SelectedValue = _objs_sel.First();
                        _objs_sel.First().Selecionado = true;
                    }

                    AtualizarControlesObjeto2D(_objs_sel);

                    selRect = new RectangleF();
                }
                else if (toolStripVertice.Checked) // Ferramenta Seleção de Vértice
                {
                    selRect = new RectangleF();
                }
                else if (toolStripOrigem.Checked) // Ferramenta Seleção de Vértice
                {
                    selRect = new RectangleF();
                }
                else if (toolStripVetor.Checked) // Ferramenta Seleção de Vértice
                {
                    selRect = new RectangleF();
                }
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (selRect.Contains(e.Location))
                    {
                        Debug.WriteLine("Clique Direito");
                    }
                }
            }
        }

        private void ToolStripSelecao_Click(object sender, EventArgs e)
        {
            ResetaFerramentasSelecao((ToolStripButton)sender);
            HabilitarFerramentasTransformacao();
            toolStripSelecao.Checked = true;
            LimparSelecoesGeometricas();
            tabControlObjeto.SelectedTab = tabObjeto;
        }

        private void ToolStripVertice_Click(object sender, EventArgs e)
        {
            ResetaFerramentasSelecao((ToolStripButton)sender);
            HabilitarFerramentasTransformacao(toolStripEscala);
            toolStripEscala.Enabled = false;
            toolStripVertice.Checked = true;
            LimparSelecoesGeometricas();
            tabControlObjeto.SelectedTab = tabVertice;

            CboVertices_SelectedIndexChanged(sender, new EventArgs()); // Exibe a vértice selecionada
        }

        private void ToolStripMove_Click(object sender, EventArgs e)
        {
            ResetaFerramentasTransformacao((ToolStripButton)sender);
            toolStripMove.Checked = true;
        }

        private void ToolStripEscala_Click(object sender, EventArgs e)
        {
            ResetaFerramentasTransformacao((ToolStripButton)sender);
            toolStripEscala.Checked = true;
        }

        private void ToolStripAngulo_Click(object sender, EventArgs e)
        {
            ResetaFerramentasTransformacao((ToolStripButton)sender);
            toolStripAngulo.Checked = true;
        }

        private void ToolStripRaio_Click(object sender, EventArgs e)
        {
            ResetaFerramentasTransformacao((ToolStripButton)sender);
            toolStripRaio.Checked = true;
        }

        /// <summary>
        /// Habilita todas as ferramentas de transformação e desabilita as ferramentas informadas no parâmetro.
        /// </summary>
        /// <param name="desabilitar"></param>
        private void HabilitarFerramentasTransformacao(params ToolStripButton[] desabilitar)
        {
            _ferramentasTransformacao.ForEach(x => x.Enabled = true);
            desabilitar.ToList().ForEach(x => x.Enabled = false);
        }

        private void HabilitarFerramentasTransformacao(bool habilitar)
        {
            _ferramentasTransformacao.ForEach(x => x.Enabled = habilitar);
        }

        private void ResetaFerramentasTransformacao(ToolStripButton exceto)
        {
            _ferramentasTransformacao.Except(new List<ToolStripButton>() { exceto })
                .ToList().ForEach(x => x.Checked = false);
        }

        private void ResetaFerramentasSelecao(ToolStripButton exceto)
        {
            _ferramentasSelecao.Except(new List<ToolStripButton>() { exceto })
                .ToList().ForEach(x => x.Checked = false);
        }

        private void ToolStripVetor_Click(object sender, EventArgs e)
        {
            ResetaFerramentasSelecao((ToolStripButton)sender);
            HabilitarFerramentasTransformacao();
            toolStripVetor.Checked = true;
            LimparSelecoesGeometricas();
            tabControlObjeto.SelectedTab = tabVetor;
        }

        private void ToolStripOrigem_Click(object sender, EventArgs e)
        {
            ResetaFerramentasSelecao((ToolStripButton)sender);
            HabilitarFerramentasTransformacao(toolStripRaio, toolStripEscala);
            toolStripOrigem.Checked = true;
            LimparSelecoesGeometricas();
            tabControlObjeto.SelectedTab = tabOrigem;

            CboOrigem_SelectedIndexChanged(sender, new EventArgs()); // Exibe o ponto de origem
        }

        private void LimparSelecoesGeometricas()
        {
            _origens_sel.ForEach(x => x.Sel = false);
            _origens_sel.Clear();

            _vetores_sel.ForEach(x => x.Sel = false);
            _vetores_sel.Clear();

            _vertices_sel.ForEach(x => x.Sel = false);
            _vertices_sel.Clear();
        }

        private void TabControlObjeto_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tabObjeto)
            {
                ToolStripSelecao_Click(toolStripSelecao, new EventArgs());
            }
            else if (e.TabPage == tabOrigem)
            {
                ToolStripOrigem_Click(toolStripOrigem, new EventArgs());
            }
            else if (e.TabPage == tabVetor)
            {
                ToolStripVetor_Click(toolStripVetor, new EventArgs());
            }
            else if (e.TabPage == tabVertice)
            {
                ToolStripVertice_Click(toolStripVertice, new EventArgs());
            }
        }

        private void CboOrigem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboOrigem.SelectedValue != null)
            {
                _origens_sel.ForEach(x => x.Sel = false);
                _origens_sel.Clear();
                _origens_sel.Add((Origem2D)cboOrigem.SelectedValue);
                if (toolStripOrigem.Checked) _origens_sel.First().Sel = true;
                AtualizarControlesPontoCentro(_origens_sel);
            }
        }

        private void AtualizarControlesPontoCentro(List<Origem2D> selecionados)
        {
            // Nenhum objeto selecionado?
            if (selecionados.Count == 0)
            {
                txtOrigemPosX.Text = string.Empty;
                txtOrigemPosY.Text = string.Empty;
            }
            else
            {
                if (selecionados.Count == 1) // Único objeto selecionado?
                {
                    txtOrigemPosX.Text = selecionados.First().X.ToString();
                    txtOrigemPosY.Text = selecionados.First().Y.ToString();
                }
                else // Muitos objetos selecionados?
                {
                    txtOrigemPosX.Text = string.Empty;
                    txtOrigemPosY.Text = string.Empty;
                }
            }
        }

        private void TxtOrigemPosX_ValueChanged(object sender, EventArgs e)
        {
            if (_origens_sel != null && txtOrigemPosX.Focused)
            {
                if (float.TryParse(txtOrigemPosX.Text, out float posX))
                {
                    _origens_sel.First().X = posX;
                }
            }
        }

        private void TxtOrigemPosY_ValueChanged(object sender, EventArgs e)
        {
            if (_origens_sel != null && txtOrigemPosY.Focused)
            {
                if (float.TryParse(txtOrigemPosY.Text, out float posY))
                {
                    _origens_sel.First().Y = posY;
                }
            }
        }

        private void BtnFocarOrigem_Click(object sender, EventArgs e)
        {
            if (cboOrigem.SelectedValue != null)
            {
                _engine2D.Camera.Focar((Origem2D)cboOrigem.SelectedValue);
            }
        }

        private void TxtVerticePosX_ValueChanged(object sender, EventArgs e)
        {
            if (_vertices_sel != null && txtVerticePosX.Focused)
            {
                if (float.TryParse(txtVerticePosX.Text, out float posX))
                {
                    // TODO: Ao alterar posY deve recalcular o angulo e o radiano com base
                    // nas novas coordenadas de PosX e PosY
                    _vertices_sel.First().X = posX;
                }
            }
        }

        private void TxtVerticePosY_ValueChanged(object sender, EventArgs e)
        {
            if (_vertices_sel != null && txtVerticePosY.Focused)
            {
                if (float.TryParse(txtVerticePosY.Text, out float posY))
                {
                    // TODO: Ao alterar posY deve recalcular o angulo e o radiano com base
                    // nas novas coordenadas de PosX e PosY
                    _vertices_sel.First().Y = posY;
                }
            }
        }

        private void TxtVerticeRaio_ValueChanged(object sender, EventArgs e)
        {

        }

        private void TxtVerticeAngulo_ValueChanged(object sender, EventArgs e)
        {

        }

        private void TxtCamAngulo_ValueChanged(object sender, EventArgs e)
        {
            if (txtCamAngulo.Focused)
            {
                if (float.TryParse(txtCamAngulo.Text, out float ang))
                {
                    _engine2D.Camera.Angulo = ang;
                }
            }
        }
    }
}
