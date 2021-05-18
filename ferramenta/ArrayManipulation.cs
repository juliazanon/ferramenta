using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ferramenta
{
    class ArrayManipulation
    {
		public static T[] GetRow<T>(T[,] matrix, int rowNumber)
		{
			return Enumerable.Range(0, matrix.GetLength(1))
				.Select(x => matrix[rowNumber, x])
				.ToArray();
		}

		// Get specific column in a jagged array
		public static T[] GetColumn<T>(T[][] jaggedArray, int wanted_column)
		{
			T[] columnArray = new T[jaggedArray.Length];
			T[] rowArray;
			for (int i = 0; i < jaggedArray.Length; i++)
			{
				rowArray = jaggedArray[i];
				if (wanted_column < rowArray.Length)
					columnArray[i] = rowArray[wanted_column];
			}
			return columnArray;
		}
	}
}
