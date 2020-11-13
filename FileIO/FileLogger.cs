using MACOs.JY.DataLogger.FileIO.Implements;
using SeeSharpTools.JY.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MACOs.JY.DataLogger.FileIO
{
    public abstract class LogWriter
    {
        #region Public Properties

        public string FilePath { get; set; }
        public List<AnalogWaveformFile> FileStreamCollection { get; internal set; } = new List<AnalogWaveformFile>();
        public Project Project { get; set; }
        public int PlotCount { get; set; }

        #endregion Public Properties

        #region Public Methods

        private static LogWriter CreateInstance(LogType type = LogType.AnalogWaveform)
        {
            switch (type)
            {
                case LogType.AnalogWaveform:
                    return new AnalogWaveformWriter();

                case LogType.Ccv:
                    return null;

                case LogType.Mat:
                    return null;

                default:
                    return null;
            }
        }

        public static LogWriter CreateInstance(Project project, LogType type = LogType.AnalogWaveform)
        {
            var logger = CreateInstance(type);
            logger.Project = project;
            if (!Directory.Exists(logger.Project.Directory))
            {
                Directory.CreateDirectory(logger.Project.Directory);
            }
            logger.Configure();
            FileAdapter.WriteConfigureFile(logger.Project);
            return logger;
        }

        public abstract void Configure();

        public abstract void WriteData(string groupName, double[,] waveform);

        public abstract void WriteData(Group group, double[,] waveform);

        public abstract void Close();

        #endregion Public Methods
    }

    public abstract class LogReader
    {
        public abstract ChartData ReadData(Channel ch, int start = 0, int len = 0);

        public abstract ChartData ReadData(Channel[] chs, int start = 0, int len = 0);

        public static LogReader CreateInstance(LogType type = LogType.AnalogWaveform)
        {
            switch (type)
            {
                case LogType.AnalogWaveform:
                    return new AnalogWaveformReader();

                case LogType.Ccv:
                    return null;

                case LogType.Mat:
                    return null;

                default:
                    return null;
            }
        }
    }

    public class ChartData
    {
        //画图的y值
        public double[,] Y
        { get; set; }

        //画图x轴的间隔
        public double dx
        { get; set; }

        //画图x轴的起始位置
        public double X0
        { get; set; }

        public void Append(double[,] newValue)
        {
            Y = ResizeArray(Y, newValue);
        }

        private T[,] ResizeArray<T>(T[,] original, T[,] newValues)
        {
            T[,] newArray;
            int col = 0;
            int row = 0;
            int size = Marshal.SizeOf(typeof(T));

            if (original == null)
            {
                col = newValues.GetLength(1);
                row = newValues.GetLength(0);
                newArray = new T[row, col];
                Buffer.BlockCopy(newValues, 0, newArray, 0, col * row * size);
            }
            else
            {
                col = Math.Max(original.GetLength(1), newValues.GetLength(1));
                row = original.GetLength(0) + newValues.GetLength(0);
                newArray = new T[row, col];
                for (int i = 0; i < original.GetLength(0); i++)
                {
                    int len = original.GetLength(1);
                    Buffer.BlockCopy(original, i * len * size, newArray, i * col * size, len * size);
                }
                for (int i = 0; i < newValues.GetLength(0); i++)
                {
                    int offset = original.GetLength(0) * col;
                    int len = newValues.GetLength(1);
                    Buffer.BlockCopy(newValues, i * len * size, newArray, (offset + i * col) * size, len * size);
                }
            }

            return newArray;
        }
    }

    public enum LogType
    {
        AnalogWaveform,
        Ccv,
        Mat,
    }
}