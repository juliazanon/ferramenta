using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph;
using GlmNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ferramenta
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
		
		/// All program global variables
		
		// Finished variables
		bool finished = false;
		bool dispCBOk = false;
		bool openFileOk = false;

		// File points lists
		List<float[]> vertexes = new List<float[]>();
		List<float[]> tensions = new List<float[]>();

		// List os displacement poins transformed to range -1 to 1
		List<float> displacementTransformed = new List<float>();

		//  Camera parameters
		float[] _viewPoint = new float[] { 0.0f, 0.0f, 80.0f };
		float[] _position = new float[] { 40.0f, 0.0f, 80.0f };
		float[] _upVector = new float[] { 0.0f, 1.0f, 0.0f };
		float _moveDistance = 1.0f;
		float scale = 0.15f;

		//  Keyboard event handler
		int keyCode = 0;


		/// WPF Events

		private void FinishButtonClick(object sender, RoutedEventArgs e)
		{
			finished = true;
			DisplacementCB.IsEnabled = false;
			HelixCB.IsEnabled = false;
		}

		private void ClearAllClick(object sender, RoutedEventArgs e)
		{
			// Clear OpenGL
			OpenGL gl = GLControl.OpenGL;
			gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			finished = false;

			// Clear WPF
			finish_btn.IsEnabled = false;
			DisplacementCB.IsEnabled = true;
			HelixCB.IsEnabled = true;
			tb.Text = "Upload File";
			btnOpenFile.IsEnabled = true;
			openFileOk = false;

			// Clear Lists
			vertexes.Clear();
			tensions.Clear();
			displacementTransformed.Clear();
		}

		private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
			// Clear OpenGL
			OpenGL gl = GLControl.OpenGL;
			gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			finished = false;

			// Clear WPF
			finish_btn.IsEnabled = false;
			DisplacementCB.IsEnabled = true;
			HelixCB.IsEnabled = true;

			// Clear Lists
			displacementTransformed.Clear();
		}

		// Open file dialog and events
		private void OpenFileClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog
			{
				DefaultExt = ".txt",  // Required file extension 
				Filter = "Text documents (.txt)|*.txt"  // Optional file extensions
			};

			bool? result = dlg.ShowDialog();
			if (result == true)
			{
				using (Stream s = dlg.OpenFile())
				{
					// Get the path of the file
					string path = dlg.FileName;
					tb.Text = path;

					// Get the number of points registered in the file, discount the first line
					int length = File.ReadAllLines(path).Length - 1;

					// Begin reading
					float[,] points = new float[length, 7];
					int count = -1;
					foreach (string line in File.ReadLines(path))
					{
						// Get the numbers in a line
						string[] numbers = line.Split();
						// Exclude the first line
						if (line[0] != char.Parse("I"))
						{
							for (int i = 0; i < 7; i++)
							{
								// Form the matrix containing all numbers
								points[count, i] = float.Parse(numbers[i], CultureInfo.InvariantCulture);
							}
						}
						count++;
					}

					// Put the wanted data in different arrays
					float[,] pointsCartesian = new float[length, 3];
					float[,] displacement = new float[length, 3];
					for (int i = 0; i < length; i++)
					{
						pointsCartesian[i, 0] = points[i, 1] * (float)Math.Cos(points[i, 2]) / 10;
						pointsCartesian[i, 1] = points[i, 1] * (float)Math.Sin(points[i, 2]) / 10;
						pointsCartesian[i, 2] = points[i, 3] / 10;

						displacement[i, 0] = points[i, 4];
						displacement[i, 1] = points[i, 5];
						displacement[i, 2] = points[i, 6];
					}

					// Add the data in external lists for further use
					for (int i = 0; i < length; i++)
					{
						vertexes.Add(ArrayManipulation.GetRow(pointsCartesian, i));
						tensions.Add(ArrayManipulation.GetRow(displacement, i));
					}

					if (path != "") { btnOpenFile.IsEnabled = false; }
					openFileOk = true;
				}
			}
		}

		private void TransformDisplacement(float[] displacementType)
		{
			// Get the absolute of all elements and add to a list
			for (int i = 0; i < displacementType.Length; i++)
			{
				displacementType[i] = Math.Abs(displacementType[i]);
			}

			float max = displacementType.Max();

			// Transform to range -1 to 1
			for (int i = 0; i < displacementType.Length; i++)
			{
				displacementTransformed.Add(displacementType[i] / (max / 2) - 1);
			}
		}

		private void DispCBChange(object sender, SelectionChangedEventArgs e)
		{
			dispCBOk = true;
		}

		private new void KeyDown(object sender, KeyEventArgs e)
		{
			// Key definitions
			if (e.Key == Key.W || e.Key == Key.Up) { keyCode = 1; }
			else if (e.Key == Key.S || e.Key == Key.Down) { keyCode = 2; }
			else if (e.Key == Key.A || e.Key == Key.Left) { keyCode = 3; }
			else if (e.Key == Key.D || e.Key == Key.Right) { keyCode = 4; }
			else if (e.Key == Key.Add || e.Key == Key.OemPlus) { keyCode = 5; }
			else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) { keyCode = 6; }
            else { keyCode = 0; }

			//  pan
			//  y axis
			//  Up
			if (keyCode == 1)
			{
				_viewPoint[1] += _moveDistance;
				_position[1] += _moveDistance;
			}
			//  Down
			else if (keyCode == 2)
			{
				_viewPoint[1] += -_moveDistance;
				_position[1] += -_moveDistance;
			}

			//  x axis
			//  Left
			else if (keyCode == 3)
			{
				_viewPoint[2] += _moveDistance;
				_position[2] += _moveDistance;
			}
			//  Right
			else if (keyCode == 4)
			{
				_viewPoint[2] += -_moveDistance;
				_position[2] += -_moveDistance;
			}
			//  zoom
			else if (keyCode == 5) {
				scale += 0.01f;
			}
			else if (keyCode == 6) {
				if (scale > 0.04) { scale -= 0.03f; }
			}
		}

		/// Main OpenGL program
		
		private void OpenGLDraw(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
		{
			// Enable finish button
			if (openFileOk && dispCBOk) { finish_btn.IsEnabled = true; }
			else { finish_btn.IsEnabled = false; }

			//  Get the OpenGL object
			OpenGL gl = GLControl.OpenGL;

			//  Set The Material Color
			float[] glfMaterialColor = new float[] { 0.4f, 0.2f, 0.8f, 1.0f };

			float w = (float)GLControl.ActualWidth;
			float h = (float)GLControl.ActualHeight;

			//  Perspective projection
			gl.Viewport(0, 0, (int)w, (int)h);

			gl.Ortho(0, w, 0, h, -100, 100);

			//  Clear The Screen And The Depth Buffer
			gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			gl.Material(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_AMBIENT_AND_DIFFUSE, glfMaterialColor);

			gl.MatrixMode(MatrixMode.Modelview);
			gl.LoadIdentity();

			gl.Scale(scale,scale,1);

			//  Camera Position
			gl.LookAt(_position[0], _position[1], _position[2],
				_viewPoint[0], _viewPoint[1], _viewPoint[2],
				_upVector[0], _upVector[1], _upVector[2]);

			// Draw the helix
			float[][] points = vertexes.ToArray();
			Helix(points.Length, points);
		}

		//  Draw the file helix
		private void Helix(int length, float[][] points)
		{
			if (finished)
			{
				int choice;
				if (DisplacementCB.SelectedIndex != -1)
				{
					choice = (int)DisplacementCB.SelectedIndex;
				}
                else
                {
					choice = 0;
                }

				// Get the array of displacements of choice
				float[] displacementType = ArrayManipulation.GetColumn(tensions.ToArray(), choice);
				TransformDisplacement(displacementType);

				OpenGL gl = GLControl.OpenGL;
				gl.Begin(BeginMode.LineStrip);
				float[] color = new float[] { 0.0f, 0.0f, 0.0f };

				float[] displacement = displacementTransformed.ToArray();
				for (int i = 0; i < length; i++)
				{
					// Colors
					switch (displacement[i])
					{
						case var n when n >= -1 && n < -0.6:
							color = new float[] { 0.0f, 0.0f, 1.0f };  // Blue
							break;
						case var n when n >= -0.6 && n < -0.3:
							color = new float[] { 0.0f, 1.0f, 1.0f };  // Cyan
							break;
						case var n when n >= -0.3 && n < 0:
							color = new float[] { 0.0f, 1.0f, 0.0f };  // Green
							break;
						case var n when n >= 0 && n < 0.3:
							color = new float[] { 1.0f, 1.0f, 0.0f };  // Yellow
							break;
						case var n when n >= 0.3 && n < 0.6:
							color = new float[] { 1.0f, 0.5f, 0.0f };  // Orange
							break;
						case var n when n >= 0.6:
							color = new float[] { 1.0f, 0.0f, 0.0f };  // Red
							break;
						default:
							break;
					}

					gl.Color(color);

					gl.Vertex(new SharpGL.SceneGraph.Vertex(points[i][0], points[i][1], points[i][2]));
				}

				gl.End();  // Done Rendering

				gl.Flush();
			}
		}

		/// Color Scale

        private void DrawColorScale(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
        {
			OpenGL gl = ColorScaleControl.OpenGL;
			gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

			float w = (float)ColorScaleControl.ActualWidth;
			float h = (float)ColorScaleControl.ActualHeight;

			gl.Viewport(0, 0, (int)w, (int)h);
			gl.MatrixMode(MatrixMode.Projection);
			gl.LoadIdentity();

			gl.MatrixMode(MatrixMode.Modelview);
			gl.LoadIdentity();

			gl.Begin(BeginMode.QuadStrip);

			gl.Color(0.0f, 0.0f, 1.0f);  // Blue
			gl.Vertex(-0.05f, 0.0f, 0.0f);
			gl.Vertex(-0.05f, 1.0f, 0.0f);

			gl.Color(0.0f, 1.0f, 1.0f);  // Cyan
			gl.Vertex(0.15f, 0.0f, 0.0f);
			gl.Vertex(0.15f, 1.0f, 0.0f);

			gl.Color(0.0f, 1.0f, 0.0f);  // Green
			gl.Vertex(0.35f, 0.0f, 0.0f);
			gl.Vertex(0.35f, 1.0f, 0.0f);

			gl.Color(1.0f, 1.0f, 0.0f);  // Yellow
			gl.Vertex(0.55f, 0.0f, 0.0f);
			gl.Vertex(0.55f, 1.0f, 0.0f);

			gl.Color(1.0f, 0.5f, 0.0f);  // Orange
			gl.Vertex(0.75f, 0.0f, 0.0f);
			gl.Vertex(0.75f, 1.0f, 0.0f);

			gl.Color(1.0f, 0.0f, 0.0f);  // Red
			gl.Vertex(0.95f, 0.0f, 0.0f);
			gl.Vertex(0.95f, 1.0f, 0.0f);

			gl.End();
			gl.Begin(BeginMode.Lines);

			gl.Color(1.0f, 1.0f, 1.0f);  // White
			gl.Vertex(-0.05f, -0.2f, 0.0f);
			gl.Vertex(-0.05f, -0.7f, 0.0f);
			gl.Vertex(0.95f, -0.2f, 0.0f);
			gl.Vertex(0.95f, -0.7f, 0.0f);
			gl.Vertex(-0.05f, -0.4f, 0.0f);
			gl.Vertex(0.95f, -0.4f, 0.0f);

			//gl.DrawText(-1, 0, 128.0f, 128.0f, 128.0f, "Arial", 1.0f, "Drawing maze");

			gl.End();
			gl.Flush();
		}
    }
}
