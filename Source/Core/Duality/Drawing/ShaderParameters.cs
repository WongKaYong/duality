﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Duality.Editor;
using Duality.Resources;
using Duality.Serialization;

namespace Duality.Drawing
{
	/// <summary>
	/// Describes a set of shader parameters independently from any specific shader.
	/// It's a CPU side key-value store for values that can be applied to a shader
	/// program by the <see cref="Duality.Backend.IGraphicsBackend"/>.
	/// </summary>
	public class ShaderParameters : IEquatable<ShaderParameters>, ISerializeExplicit
	{
		private struct NameComparer : IComparer<ValueItem>
		{
			public int Compare(ValueItem x, ValueItem y)
			{
				return string.CompareOrdinal(x.Name, y.Name);
			}
		}
		private struct ValueItem
		{
			public string Name;
			public ContentRef<Texture> Texture;
			public float[] Uniform;
		}

		private static readonly IComparer<ValueItem> nameComparer = new NameComparer();

		private RawList<ValueItem> values = null;
		private ulong              hash   = 0L;
		
		
		/// <summary>
		/// [GET / SET] Shortcut for accessing the <see cref="ShaderFieldInfo.DefaultNameMainTex"/> texture variable.
		/// </summary>
		public ContentRef<Texture> MainTexture
		{
			get { return this.GetTexture(ShaderFieldInfo.DefaultNameMainTex); }
			set { this.SetTexture(ShaderFieldInfo.DefaultNameMainTex, value); }
		}
		/// <summary>
		/// [GET] A 64 bit hash value that represents this particular collection of
		/// shader parameters. The same set of parameters will always have the same
		/// hash value.
		/// </summary>
		public ulong Hash
		{
			get { return this.hash; }
		}


		public ShaderParameters()
		{
			this.UpdateHash();
		}
		public ShaderParameters(ShaderParameters other)
		{
			if (other.values != null)
			{
				this.values = new RawList<ValueItem>(other.values);
				int count = this.values.Count;
				ValueItem[] data = this.values.Data;
				for (int i = 0; i < data.Length; i++)
				{
					if (i > count) break;
					if (data[i].Uniform == null) continue;
					data[i].Uniform = (float[])data[i].Uniform.Clone();
				}
			}
			this.hash = other.hash;
		}

		/// <summary>
		/// Removes all variables and values from the <see cref="ShaderParameters"/> instance.
		/// </summary>
		public void Clear()
		{
			if (this.values != null)
				this.values.Clear();
			this.UpdateHash();
		}
		/// <summary>
		/// Removes a variable from storage inside this <see cref="ShaderParameters"/> instance.
		/// </summary>
		/// <param name="name"></param>
		public void Remove(string name)
		{
			int index = this.FindIndex(name);
			if (index != -1) this.values.RemoveAt(index);
			this.UpdateHash();
		}
		
		/// <summary>
		/// Assigns an array of values to the specified variable. All values are copied and converted into
		/// a shared internal format.
		/// 
		/// Supported base types are <see cref="Single"/>, <see cref="Vector2"/>, <see cref="Vector3"/>, 
		/// <see cref="Vector4"/>, <see cref="Matrix3"/>, <see cref="Matrix4"/>, <see cref="Int32"/>,
		/// <see cref="Point2"/> and <see cref="Boolean"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetArray<T>(string name, T[] value) where T : struct
		{
			if (string.IsNullOrEmpty(name)) ThrowInvalidName();;
			if (value == null || value.Length == 0) ThrowInvalidValue();

			float[] rawData;
			if (typeof(T) == typeof(float))
			{
				float[] typedValue = (float[])(object)value;
				this.EnsureUniformData(name, value.Length * 1, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i] = typedValue[i];
				}
			}
			else if (typeof(T) == typeof(Vector2))
			{
				Vector2[] typedValue = (Vector2[])(object)value;
				this.EnsureUniformData(name, value.Length * 2, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 2 + 0] = typedValue[i].X;
					rawData[i * 2 + 1] = typedValue[i].Y;
				}
			}
			else if (typeof(T) == typeof(Vector3))
			{
				Vector3[] typedValue = (Vector3[])(object)value;
				this.EnsureUniformData(name, value.Length * 3, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 3 + 0] = typedValue[i].X;
					rawData[i * 3 + 1] = typedValue[i].Y;
					rawData[i * 3 + 2] = typedValue[i].Z;
				}
			}
			else if (typeof(T) == typeof(Vector4))
			{
				Vector4[] typedValue = (Vector4[])(object)value;
				this.EnsureUniformData(name, value.Length * 4, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 4 + 0] = typedValue[i].X;
					rawData[i * 4 + 1] = typedValue[i].Y;
					rawData[i * 4 + 2] = typedValue[i].Z;
					rawData[i * 4 + 3] = typedValue[i].W;
				}
			}
			else if (typeof(T) == typeof(Matrix3))
			{
				Matrix3[] typedValue = (Matrix3[])(object)value;
				this.EnsureUniformData(name, value.Length * 9, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 9 + 0] = typedValue[i].Row0.X;
					rawData[i * 9 + 1] = typedValue[i].Row0.Y;
					rawData[i * 9 + 2] = typedValue[i].Row0.Z;
					rawData[i * 9 + 3] = typedValue[i].Row1.X;
					rawData[i * 9 + 4] = typedValue[i].Row1.Y;
					rawData[i * 9 + 5] = typedValue[i].Row1.Z;
					rawData[i * 9 + 6] = typedValue[i].Row2.X;
					rawData[i * 9 + 7] = typedValue[i].Row2.Y;
					rawData[i * 9 + 8] = typedValue[i].Row2.Z;
				}
			}
			else if (typeof(T) == typeof(Matrix4))
			{
				Matrix4[] typedValue = (Matrix4[])(object)value;
				this.EnsureUniformData(name, value.Length * 16, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 16 +  0] = typedValue[i].Row0.X;
					rawData[i * 16 +  1] = typedValue[i].Row0.Y;
					rawData[i * 16 +  2] = typedValue[i].Row0.Z;
					rawData[i * 16 +  3] = typedValue[i].Row0.W;
					rawData[i * 16 +  4] = typedValue[i].Row1.X;
					rawData[i * 16 +  5] = typedValue[i].Row1.Y;
					rawData[i * 16 +  6] = typedValue[i].Row1.Z;
					rawData[i * 16 +  7] = typedValue[i].Row1.W;
					rawData[i * 16 +  8] = typedValue[i].Row2.X;
					rawData[i * 16 +  9] = typedValue[i].Row2.Y;
					rawData[i * 16 + 10] = typedValue[i].Row2.Z;
					rawData[i * 16 + 11] = typedValue[i].Row2.W;
					rawData[i * 16 + 12] = typedValue[i].Row3.X;
					rawData[i * 16 + 13] = typedValue[i].Row3.Y;
					rawData[i * 16 + 14] = typedValue[i].Row3.Z;
					rawData[i * 16 + 15] = typedValue[i].Row3.W;
				}
			}
			else if (typeof(T) == typeof(int))
			{
				int[] typedValue = (int[])(object)value;
				this.EnsureUniformData(name, value.Length * 1, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i] = typedValue[i];
				}
			}
			else if (typeof(T) == typeof(Point2))
			{
				Point2[] typedValue = (Point2[])(object)value;
				this.EnsureUniformData(name, value.Length * 2, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i * 2 + 0] = typedValue[i].X;
					rawData[i * 2 + 1] = typedValue[i].Y;
				}
			}
			else if (typeof(T) == typeof(bool))
			{
				bool[] typedValue = (bool[])(object)value;
				this.EnsureUniformData(name, value.Length * 1, out rawData);
				for (int i = 0; i < value.Length; i++)
				{
					rawData[i] = typedValue[i] ? 1.0f : 0.0f;
				}
			}
			else
			{
				ThrowUnsupportedValueType<T>();
			}

			this.UpdateHash();
		}
		/// <summary>
		/// Assigns a blittable value to the specified variable. All values are copied and converted into
		/// a shared internal format.
		/// 
		/// Supported base types are <see cref="Single"/>, <see cref="Vector2"/>, <see cref="Vector3"/>, 
		/// <see cref="Vector4"/>, <see cref="Matrix3"/>, <see cref="Matrix4"/>, <see cref="Int32"/>,
		/// <see cref="Point2"/> and <see cref="Boolean"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetValue<T>(string name, T value) where T : struct
		{
			if (string.IsNullOrEmpty(name)) ThrowInvalidName();

			float[] rawData;
			if (typeof(T) == typeof(float))
			{
				float typedValue = (float)(object)value;
				this.EnsureUniformData(name, 1, out rawData);
				rawData[0] = typedValue;
			}
			else if (typeof(T) == typeof(Vector2))
			{
				Vector2 typedValue = (Vector2)(object)value;
				this.EnsureUniformData(name, 2, out rawData);
				rawData[0] = typedValue.X;
				rawData[1] = typedValue.Y;
			}
			else if (typeof(T) == typeof(Vector3))
			{
				Vector3 typedValue = (Vector3)(object)value;
				this.EnsureUniformData(name, 3, out rawData);
				rawData[0] = typedValue.X;
				rawData[1] = typedValue.Y;
				rawData[2] = typedValue.Z;
			}
			else if (typeof(T) == typeof(Vector4))
			{
				Vector4 typedValue = (Vector4)(object)value;
				this.EnsureUniformData(name, 4, out rawData);
				rawData[0] = typedValue.X;
				rawData[1] = typedValue.Y;
				rawData[2] = typedValue.Z;
				rawData[3] = typedValue.W;
			}
			else if (typeof(T) == typeof(Matrix3))
			{
				Matrix3 typedValue = (Matrix3)(object)value;
				this.EnsureUniformData(name, 9, out rawData);
				rawData[0] = typedValue.Row0.X;
				rawData[1] = typedValue.Row0.Y;
				rawData[2] = typedValue.Row0.Z;
				rawData[3] = typedValue.Row1.X;
				rawData[4] = typedValue.Row1.Y;
				rawData[5] = typedValue.Row1.Z;
				rawData[6] = typedValue.Row2.X;
				rawData[7] = typedValue.Row2.Y;
				rawData[8] = typedValue.Row2.Z;
			}
			else if (typeof(T) == typeof(Matrix4))
			{
				Matrix4 typedValue = (Matrix4)(object)value;
				this.EnsureUniformData(name, 16, out rawData);
				rawData[ 0] = typedValue.Row0.X;
				rawData[ 1] = typedValue.Row0.Y;
				rawData[ 2] = typedValue.Row0.Z;
				rawData[ 3] = typedValue.Row0.W;
				rawData[ 4] = typedValue.Row1.X;
				rawData[ 5] = typedValue.Row1.Y;
				rawData[ 6] = typedValue.Row1.Z;
				rawData[ 7] = typedValue.Row1.W;
				rawData[ 8] = typedValue.Row2.X;
				rawData[ 9] = typedValue.Row2.Y;
				rawData[10] = typedValue.Row2.Z;
				rawData[11] = typedValue.Row2.W;
				rawData[12] = typedValue.Row3.X;
				rawData[13] = typedValue.Row3.Y;
				rawData[14] = typedValue.Row3.Z;
				rawData[15] = typedValue.Row3.W;
			}
			else if (typeof(T) == typeof(int))
			{
				int typedValue = (int)(object)value;
				this.EnsureUniformData(name, 1, out rawData);
				rawData[0] = typedValue;
			}
			else if (typeof(T) == typeof(Point2))
			{
				Point2 typedValue = (Point2)(object)value;
				this.EnsureUniformData(name, 2, out rawData);
				rawData[0] = typedValue.X;
				rawData[1] = typedValue.Y;
			}
			else if (typeof(T) == typeof(bool))
			{
				bool typedValue = (bool)(object)value;
				this.EnsureUniformData(name, 1, out rawData);
				rawData[0] = typedValue ? 1.0f : 0.0f;
			}
			else
			{
				ThrowUnsupportedValueType<T>();
			}

			this.UpdateHash();
		}
		/// <summary>
		/// Assigns a texture to the specified variable.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetTexture(string name, ContentRef<Texture> value)
		{
			if (string.IsNullOrEmpty(name)) ThrowInvalidName();
			
			int index = this.FindIndex(name);
			if (index == -1)
			{
				if (this.values == null)
					this.values = new RawList<ValueItem>();
				this.values.Add(new ValueItem
				{
					Name = name,
					Texture = value
				});
				this.EnsureSortedByName();
			}
			else
			{
				this.values.Data[index].Texture = value;
				this.values.Data[index].Uniform = null;
			}

			this.UpdateHash();
		}
		
		/// <summary>
		/// Retrieves a copy of the values that are assigned the specified variable. If the internally 
		/// stored type does not match the specified type, it will be converted before returning.
		/// 
		/// Supported base types are <see cref="Single"/>, <see cref="Vector2"/>, <see cref="Vector3"/>, 
		/// <see cref="Vector4"/>, <see cref="Matrix3"/>, <see cref="Matrix4"/>, <see cref="Int32"/>,
		/// <see cref="Point2"/> and <see cref="Boolean"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T[] GetArray<T>(string name) where T : struct
		{
			if (string.IsNullOrEmpty(name)) return null;

			float[] rawData = this.GetInternalData(name);
			if (typeof(T) == typeof(float))
			{
				if (rawData == null || rawData.Length < 1) return null;
				float[] result = new float[rawData.Length / 1];
				for (int i = 0; i < result.Length; i++)
				{
					result[i] = rawData[i];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Vector2))
			{
				if (rawData == null || rawData.Length < 2) return null;
				Vector2[] result = new Vector2[rawData.Length / 2];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].X = rawData[i * 2 + 0];
					result[i].Y = rawData[i * 2 + 1];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Vector3))
			{
				if (rawData == null || rawData.Length < 3) return null;
				Vector3[] result = new Vector3[rawData.Length / 3];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].X = rawData[i * 3 + 0];
					result[i].Y = rawData[i * 3 + 1];
					result[i].Z = rawData[i * 3 + 2];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Vector4))
			{
				if (rawData == null || rawData.Length < 4) return null;
				Vector4[] result = new Vector4[rawData.Length / 4];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].X = rawData[i * 4 + 0];
					result[i].Y = rawData[i * 4 + 1];
					result[i].Z = rawData[i * 4 + 2];
					result[i].W = rawData[i * 4 + 3];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Matrix3))
			{
				if (rawData == null || rawData.Length < 9) return null;
				Matrix3[] result = new Matrix3[rawData.Length / 9];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].Row0.X = rawData[i * 9 + 0];
					result[i].Row0.Y = rawData[i * 9 + 1];
					result[i].Row0.Z = rawData[i * 9 + 2];
					result[i].Row1.X = rawData[i * 9 + 3];
					result[i].Row1.Y = rawData[i * 9 + 4];
					result[i].Row1.Z = rawData[i * 9 + 5];
					result[i].Row2.X = rawData[i * 9 + 6];
					result[i].Row2.Y = rawData[i * 9 + 7];
					result[i].Row2.Z = rawData[i * 9 + 8];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Matrix4))
			{
				if (rawData == null || rawData.Length < 16) return null;
				Matrix4[] result = new Matrix4[rawData.Length / 16];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].Row0.X = rawData[i * 16 +  0];
					result[i].Row0.Y = rawData[i * 16 +  1];
					result[i].Row0.Z = rawData[i * 16 +  2];
					result[i].Row0.W = rawData[i * 16 +  3];
					result[i].Row1.X = rawData[i * 16 +  4];
					result[i].Row1.Y = rawData[i * 16 +  5];
					result[i].Row1.Z = rawData[i * 16 +  6];
					result[i].Row1.W = rawData[i * 16 +  7];
					result[i].Row2.X = rawData[i * 16 +  8];
					result[i].Row2.Y = rawData[i * 16 +  9];
					result[i].Row2.Z = rawData[i * 16 + 10];
					result[i].Row2.W = rawData[i * 16 + 11];
					result[i].Row3.X = rawData[i * 16 + 12];
					result[i].Row3.Y = rawData[i * 16 + 13];
					result[i].Row3.Z = rawData[i * 16 + 14];
					result[i].Row3.W = rawData[i * 16 + 15];
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(int))
			{
				if (rawData == null || rawData.Length < 1) return null;
				int[] result = new int[rawData.Length / 1];
				for (int i = 0; i < result.Length; i++)
				{
					result[i] = MathF.RoundToInt(rawData[i]);
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(Point2))
			{
				if (rawData == null || rawData.Length < 2) return null;
				Point2[] result = new Point2[rawData.Length / 2];
				for (int i = 0; i < result.Length; i++)
				{
					result[i].X = MathF.RoundToInt(rawData[i * 2 + 0]);
					result[i].Y = MathF.RoundToInt(rawData[i * 2 + 1]);
				}
				return (T[])(object)result;
			}
			else if (typeof(T) == typeof(bool))
			{
				if (rawData == null || rawData.Length < 1) return null;
				bool[] result = new bool[rawData.Length / 1];
				for (int i = 0; i < result.Length; i++)
				{
					result[i] = rawData[i] != 0.0f;
				}
				return (T[])(object)result;
			}
			else
			{
				ThrowUnsupportedValueType<T>();
				return null;
			}
		}
		/// <summary>
		/// Retrieves a blittable value from the specified variable. All values are copied and converted into
		/// a shared internal format.
		/// 
		/// Supported base types are <see cref="Single"/>, <see cref="Vector2"/>, <see cref="Vector3"/>, 
		/// <see cref="Vector4"/>, <see cref="Matrix3"/>, <see cref="Matrix4"/>, <see cref="Int32"/>,
		/// <see cref="Point2"/> and <see cref="Boolean"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T GetValue<T>(string name) where T : struct
		{
			if (string.IsNullOrEmpty(name)) return default(T);

			float[] rawData = this.GetInternalData(name);
			if (typeof(T) == typeof(float))
			{
				if (rawData == null || rawData.Length < 1) return default(T);
				return (T)(object)rawData[0];
			}
			else if (typeof(T) == typeof(Vector2))
			{
				if (rawData == null || rawData.Length < 2) return default(T);
				return (T)(object)new Vector2(rawData[0], rawData[1]);
			}
			else if (typeof(T) == typeof(Vector3))
			{
				if (rawData == null || rawData.Length < 3) return default(T);
				return (T)(object)new Vector3(rawData[0], rawData[1], rawData[2]);
			}
			else if (typeof(T) == typeof(Vector4))
			{
				if (rawData == null || rawData.Length < 4) return default(T);
				return (T)(object)new Vector4(rawData[0], rawData[1], rawData[2], rawData[3]);
			}
			else if (typeof(T) == typeof(Matrix3))
			{
				if (rawData == null || rawData.Length < 9) return default(T);
				return (T)(object)new Matrix3(
					rawData[0], rawData[1], rawData[2], 
					rawData[3], rawData[4], rawData[5], 
					rawData[6], rawData[7], rawData[8]);
			}
			else if (typeof(T) == typeof(Matrix4))
			{
				if (rawData == null || rawData.Length < 16) return default(T);
				return (T)(object)new Matrix4(
					rawData[0], rawData[1], rawData[2], rawData[3], 
					rawData[4], rawData[5], rawData[6], rawData[7], 
					rawData[8], rawData[9], rawData[10], rawData[11], 
					rawData[12], rawData[13], rawData[14], rawData[15]);
			}
			else if (typeof(T) == typeof(int))
			{
				if (rawData == null || rawData.Length < 1) return default(T);
				return (T)(object)MathF.RoundToInt(rawData[0]);
			}
			else if (typeof(T) == typeof(Point2))
			{
				if (rawData == null || rawData.Length < 2) return default(T);
				return (T)(object)new Point2(
					MathF.RoundToInt(rawData[0]), 
					MathF.RoundToInt(rawData[1]));
			}
			else if (typeof(T) == typeof(bool))
			{
				if (rawData == null || rawData.Length < 1) return default(T);
				return (T)(object)(rawData[0] != 0.0f);
			}
			else
			{
				ThrowUnsupportedValueType<T>();
				return default(T);
			}
		}
		/// <summary>
		/// Retrieves a texture from the specified variable.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ContentRef<Texture> GetTexture(string name)
		{
			return this.GetInternalTexture(name);
		}

		/// <summary>
		/// Retrieves the internal representation of the specified variables numeric value.
		/// The returned array should be treated as read-only.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public float[] GetInternalData(string name)
		{
			int index = this.FindIndex(name);
			if (index == -1)
				return null;
			else
				return this.values.Data[index].Uniform;
		}
		/// <summary>
		/// Retrieves the internal representation of the specified variables texture value.
		/// The returned value should be treated as read-only.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ContentRef<Texture> GetInternalTexture(string name)
		{
			int index = this.FindIndex(name);
			if (index == -1)
				return null;
			else
				return this.values.Data[index].Texture;
		}

		private int FindIndex(string name)
		{
			if (this.values == null) return -1;

			int count = this.values.Count;
			ValueItem[] data = this.values.Data;

			// Do a binary search, implemented manually so we can compare
			// names directly without copying entire value elements in the process
			// and invoking delegates.
			int lowerBound = 0;
			int upperBound = count - 1;
			while (lowerBound <= upperBound)
			{
				int mid = lowerBound + ((upperBound - lowerBound) / 2);
				int compareResult = string.CompareOrdinal(data[mid].Name, name);
 
				if (compareResult == 0)
					return mid;
				else if (compareResult < 0)
					lowerBound = mid + 1;
				else
					upperBound = mid - 1;
			}
 
			return -1;
		}
		private void EnsureSortedByName()
		{
			Array.Sort(
				this.values.Data, 
				0, 
				this.values.Count, 
				nameComparer);
		}
		private void EnsureUniformData(string name, int size, out float[] data)
		{
			int index = this.FindIndex(name);
			if (index == -1)
			{
				data = new float[size];
				if (this.values == null)
					this.values = new RawList<ValueItem>();
				this.values.Add(new ValueItem
				{
					Name = name,
					Uniform = data
				});
				this.EnsureSortedByName();
			}
			else
			{
				data = this.values.Data[index].Uniform;
				if (data == null || data.Length != size)
				{
					data = new float[size];
					this.values.Data[index].Uniform = data;
					this.values.Data[index].Texture = null;
				}
			}
		}

		private void UpdateHash()
		{
			this.hash = 17L;
			unchecked
			{
				// Note: For increased flexibility in internal storage, this hash
				// algorithm does not depend in item order.
				if (this.values != null)
				{
					int count = this.values.Count;
					ValueItem[] data = this.values.Data;
					for (int i = 0; i < data.Length; i++)
					{
						if (i >= count) break;

						if (data[i].Uniform != null)
						{
							ulong localHash = 37L;
							localHash = localHash * 41L + (ulong)data[i].Name.GetHashCode();
							for (int k = 0; k < data[i].Uniform.Length; k++)
							{
								localHash = localHash * 43L + (ulong)data[i].Uniform[k].GetHashCode();
							}

							this.hash ^= localHash;
						}
						else
						{
							ulong localHash = 23L;
							localHash = localHash * 29L + (ulong)data[i].Name.GetHashCode();
							localHash = localHash * 31L + (ulong)data[i].Texture.GetHashCode();

							this.hash ^= localHash;
						}
					}
				}
			}
		}
		public override int GetHashCode()
		{
			unchecked
			{
				return 
					(int)((ulong)this.hash) ^ 
					(int)((ulong)this.hash >> 32);
			}
		}
		public override bool Equals(object obj)
		{
			ShaderParameters other = obj as ShaderParameters;
			if (other != null)
				return this.Equals(other);
			else
				return false;
		}
		public bool Equals(ShaderParameters other)
		{
			// Quick equality heuristic for perf reasons by comparing hashes only.
			// This will fail on hash collisions. However, given that the number
			// of different materials at any time tends to be low for perf reasons,
			// collisions should be unlikely enough for this optimization to hold.
			return this.hash == other.hash;
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			
			ContentRef<Texture> mainTex = this.MainTexture;
			int texCount = 0;
			int uniformCount = 0;

			// Count textures and uniforms
			if (this.values != null)
			{
				int count = this.values.Count;
				ValueItem[] data = this.values.Data;
				for (int i = 0; i < data.Length; i++)
				{
					if (i >= count) break;
					if (data[i].Uniform != null)
						uniformCount++;
					else
						texCount++;
				}
			}

			if (mainTex != null)
			{
				builder.Append(ShaderFieldInfo.DefaultNameMainTex);
				builder.Append(" \"");
				builder.Append(mainTex.Name);
				builder.Append('"');

				if (texCount > 1)
				{
					builder.Append(", +");
					builder.Append(texCount - 1);
					builder.Append(" textures");
				}
			}
			else
			{
				builder.Append(texCount);
				builder.Append(" textures");
			}

			if (uniformCount > 0)
			{
				if (builder.Length != 0) builder.Append(", ");
				builder.Append(uniformCount);
				builder.Append(" uniforms");
			}

			return builder.ToString();
		}

		void ISerializeExplicit.WriteData(IDataWriter writer)
		{
			if (this.values != null)
			{
				int count = this.values.Count;
				ValueItem[] data = this.values.Data;
				for (int i = 0; i < data.Length; i++)
				{
					if (i >= count) break;
					if (data[i].Uniform != null)
					{
						writer.WriteValue(data[i].Name, data[i].Uniform);
					}
					else
					{
						writer.WriteValue(data[i].Name, data[i].Texture);
					}
				}
			}
		}
		void ISerializeExplicit.ReadData(IDataReader reader)
		{
			if (this.values == null)
				this.values = new RawList<ValueItem>();
			else
				this.values.Clear();

			foreach (string key in reader.Keys)
			{
				object value = reader.ReadValue(key);
				if (value is ContentRef<Texture>)
				{
					ContentRef<Texture> tex = (ContentRef<Texture>)value;
					tex.MakeAvailable();
					this.values.Add(new ValueItem
					{
						Name = key,
						Texture = tex
					});
				}
				else if (value is float[])
				{
					this.values.Add(new ValueItem
					{
						Name = key,
						Uniform = (float[])value
					});
				}
			}

			this.EnsureSortedByName();
			this.UpdateHash();
		}

		private static void ThrowInvalidName()
		{
			throw new ArgumentException("The name parameter cannot be null or empty.", "name");
		}
		private static void ThrowInvalidValue()
		{
			throw new ArgumentException("The array of parameter values cannot be null or empty.", "value");
		}
		private static void ThrowUnsupportedValueType<T>()
		{
			throw new NotSupportedException(string.Format(
				"Getting or setting shader parameters as values of type {0} is not supported.", 
				typeof(T).Name));
		}
	}
}