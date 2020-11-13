using SeeSharpTools.JY.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MACOs.JY.DataLogger.FileIO.Implements
{
    public class AnalogWaveformWriter : LogWriter
    {
        internal AnalogWaveformWriter()
        {
        }

        public override void Configure()
        {
            try
            {
                var project = base.Project;
                for (int i = 0; i < project.Groups.Count; i++)
                {
                    string wvfFilePath = project.Directory + @"\" + project.Groups[i].Name + ".wvf";
                    AnalogWaveformFile file;
                    FileOperation fo = FileOperation.Create;

                    file = new AnalogWaveformFile(wvfFilePath, fo);//新建wvf文件实例
                    file.Channels = new List<ChannelInfo>();
                    for (int j = 0; j < project.Groups[i].Channels.Count; j++)
                    {
                        ChannelInfo ci = new ChannelInfo();
                        ci.Name = project.Groups[i].Channels[j].Name;//配置通道的名字
                        ci.Description = project.Groups[i].Channels[j].Description;
                        ci.Scale = project.Groups[i].Channels[j].Sacle;
                        ci.Offset = project.Groups[i].Channels[j].Offset;
                        file.Channels.Add(ci);
                    }
                    file.SampleRate = project.Groups[i].SampleRate;//配置wvf文档属性
                    file.NumberOfChannels = project.Groups[i].Channels.Count;
                    file.ArchiveInformation.Description = project.Groups[i].Description;
                    file.ArchiveInformation.DataGroupID = project.Groups[i].Name;
                    file.DataStartTime = DateTime.Now;

                    base.FileStreamCollection.Add(file);//把配置好的wvf文件放入wvf文档的列表中。
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public override void WriteData(string groupName, double[,] waveform)
        {
            try
            {
                AnalogWaveformFile fileHandle = base.FileStreamCollection.Find(x => x.ArchiveInformation.DataGroupID == groupName);//找到对应组名的wvf文件
                fileHandle.AddTimeLabel(fileHandle.TimeLabels.Count.ToString(), fileHandle.DataLength, DateTime.Now, string.Format("{0} x {1} array", fileHandle.Channels.Count, fileHandle.ArchiveInformation.Description));//添加时间标签
                fileHandle.Write(waveform);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public override void WriteData(Group group, double[,] waveform)
        {
            try
            {
                AnalogWaveformFile fileHandle = base.FileStreamCollection.Find(x => x.ArchiveInformation.DataGroupID == group.Name);//找到对应组名的wvf文件
                fileHandle.AddTimeLabel(fileHandle.TimeLabels.Count.ToString(), fileHandle.DataLength, DateTime.Now, string.Format("{0} x {1} array", fileHandle.Channels.Count, fileHandle.ArchiveInformation.Description));//添加时间标签
                fileHandle.Write(waveform);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public override void Close()
        {
            try
            {
                foreach (AnalogWaveformFile item in base.FileStreamCollection)
                {
                    item.Close();//关闭所有wvf文件
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    public class AnalogWaveformReader : LogReader
    {
        internal AnalogWaveformReader()
        {
        }

        public override ChartData ReadData(Channel ch, int start = 0, int len = 0)
        {
            var chartData = new ChartData();//实例化绘图数据
            try
            {
                Func<double, double> func = new Func<double, double>(y => ch.Sacle * y + ch.Offset);

                var group = ch.Group;
                string wvfFilePath = group.Project.Directory + @"\" + group.Name + ".wvf";
                AnalogWaveformFile anaWavFile;
                FileOperation fo = FileOperation.OpenWithReadOnly;

                anaWavFile = new AnalogWaveformFile(wvfFilePath, fo);//新建wvf文件实例
                int[] indices = new int[] { ch.Group.Channels.IndexOf(ch) };
                long totalLength_perChannel = anaWavFile.DataLength / anaWavFile.NumberOfChannels;

                if (anaWavFile.DataLength <= 0 || totalLength_perChannel < len + start || start < 0)
                {
                    //assign range is out of the boundary
                    throw new Exception("Assigned range is out of boundary");
                }

                len = len <= 0 ? (int)totalLength_perChannel - start : len;
                int frameLen = group.FrameLength;
                int iteration = len / frameLen;
                int chCount = anaWavFile.NumberOfChannels;
                double[] readData = new double[anaWavFile.Channels.Count * frameLen];
                double[,] yData = new double[indices.Length, len];//所有文件的数据
                int dataSize = sizeof(double);

                int startFrameIdx = (int)Math.Floor((double)start / frameLen);
                int stopFrameIdx = (int)Math.Ceiling((double)(start + len) / frameLen);
                int cropStart = 0;
                int cropLength = 1;
                int currPos = 0;
                for (int i = startFrameIdx; i < stopFrameIdx; i++)
                {
                    //start and stop index might not be integer of framelength, cropping data is needed
                    if (i == startFrameIdx)
                    {
                        //first iteration
                        cropStart = start % frameLen;
                        cropLength = frameLen - start % frameLen;
                    }
                    else if (i == stopFrameIdx - 1)
                    {
                        //last iteration
                        cropStart = 0;
                        cropLength = (start + len) % frameLen;
                    }
                    else
                    {
                        cropStart = 0;
                        cropLength = frameLen;
                    }
                    anaWavFile.SetFilePosition(startFrameIdx * frameLen * chCount * i);
                    anaWavFile.Read(readData);

                    for (int j = 0; j < indices.Length; j++)
                    {
                        Buffer.BlockCopy(readData, (indices[j] * frameLen + cropStart) * dataSize, yData, (currPos + j * len) * dataSize, cropLength * dataSize);
                    }
                    currPos += cropLength;
                }
                Scaling(ref yData, func);
                chartData.Y = yData;
                chartData.dx = 1 / anaWavFile.SampleRate;
                chartData.X0 = 0;
                anaWavFile.Close();
                GC.Collect();
                return chartData;
            }
            catch (Exception ex)
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo hresultFieldInfo = typeof(Exception).GetField("_HResult", flags);

                int errcode = (int)hresultFieldInfo.GetValue(ex);
                if (errcode != -2147024809)
                {
                    throw new Exception(ex.Message);
                }
                return null;
            }
        }

        public override ChartData ReadData(Channel[] chs, int start = 0, int len = 0)
        {
            var chartData = new ChartData();//实例化绘图数据
            try
            {
                if (chs.Select(x => x.Group).Distinct().Count() != 1)
                {
                    //channel collection belongs to more than 1 groups or no groups
                    throw new Exception("Channel collection should belong to same group");
                }

                Func<double, double>[] funcs = chs.Select(x => new Func<double, double>(y => x.Sacle * y + x.Offset)).ToArray();
                var group = chs[0].Group;
                string wvfFilePath = group.Project.Directory + @"\" + group.Name + ".wvf";
                AnalogWaveformFile anaWavFile;
                FileOperation fo = FileOperation.OpenWithReadOnly;

                anaWavFile = new AnalogWaveformFile(wvfFilePath, fo);//新建wvf文件实例
                int[] indices = chs.Select(x => x.Group.Channels.IndexOf(x)).ToArray();
                long totalLength_perChannel = anaWavFile.DataLength / anaWavFile.NumberOfChannels;

                if (anaWavFile.DataLength <= 0 || totalLength_perChannel < len + start || start < 0)
                {
                    //assign range is out of the boundary
                    throw new Exception("Assigned range is out of boundary");
                }

                len = len <= 0 ? (int)totalLength_perChannel - start : len;

                double[] readData = new double[anaWavFile.Channels.Count * len];
                double[,] yData = new double[indices.Length, len];//所有文件的数据
                int dataSize = sizeof(double);
                anaWavFile.SetFilePosition(start);
                anaWavFile.Read(readData);

                for (int i = 0; i < indices.Length; i++)
                {
                    Buffer.BlockCopy(readData, indices[i] * len * dataSize, yData, i * len * dataSize, len * dataSize);
                }
                Scaling(ref yData, funcs);

                chartData.Y = yData;
                chartData.dx = 1 / anaWavFile.SampleRate;
                chartData.X0 = 0;
                anaWavFile.Close();
                GC.Collect();
                return chartData;
            }
            catch (Exception ex)
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo hresultFieldInfo = typeof(Exception).GetField("_HResult", flags);

                int errcode = (int)hresultFieldInfo.GetValue(ex);
                if (errcode != -2147024809)
                {
                    throw new Exception(ex.Message);
                }
                return null;
            }
        }

        private void Scaling(ref double[,] data, Func<double, double>[] func)
        {
            if (data.GetLength(0) != func.Length)
            {
                throw new Exception("data length is not matches the number of functions");
            }

            int rowNum = data.GetLength(0);
            int colNum = data.GetLength(1);
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    data[i, j] = func[i].Invoke(data[i, j]);
                }
            }
        }

        private void Scaling(ref double[,] data, Func<double, double> func)
        {
            int rowNum = data.GetLength(0);
            int colNum = data.GetLength(1);
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    data[i, j] = func.Invoke(data[i, j]);
                }
            }
        }
    }
}