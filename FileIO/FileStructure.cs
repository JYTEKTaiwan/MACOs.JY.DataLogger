using SeeSharpTools.JY.File;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MACOs.JY.DataLogger.FileIO
{
    #region Layer 1 -- Project

    [Serializable]
    public class Project
    {
        #region Private Fields

        private GroupCollection _groupList;

        #endregion Private Fields

        #region Public Properties

        [Category("General")]
        public string Name { get; set; }

        [Category("General")]
        public string Directory { get; set; }

        [Category("General")]
        public string Path { get { return this.Name + @".xml"; } }

        [Category("Advance")]
        public string Description
        {
            get; set;
        }

        public GroupCollection Groups
        {
            get { return _groupList; }
        }

        #endregion Public Properties

        #region Constructor

        public Project()
        {
            _groupList = new GroupCollection();
            _groupList.OnAdd += _groups_OnAdd;
            string currentTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            this.Name = currentTimeStamp;

            this.Directory = @"..\Log\Data\" + currentTimeStamp;
        }

        public Project(string prjName, string logDirectory)
        {
            _groupList = new GroupCollection();
            _groupList.OnAdd += _groups_OnAdd;
            string currentTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            this.Name = string.IsNullOrEmpty(prjName) ? currentTimeStamp : prjName;

            if (string.IsNullOrEmpty(logDirectory))
            {
                this.Directory = @".\Data\";
            }
            else
            {
                this.Directory = logDirectory + @"\Log\";
            }
        }

        #endregion Constructor

        public Group CreateNewGroup(string name, int frameLength, double sampleRate)
        {
            var g = new Group();
            g.Name = name;
            g.FrameLength = frameLength;
            g.SampleRate = sampleRate;
            _groupList.Add(g);
            return g;
        }

        #region Assign the parent reference when group is added to list

        private void _groups_OnAdd(object sender, GroupAddedEventArg e)
        {
            e.Group.ConfigureParent(this);
        }

        public sealed class GroupAddedEventArg : EventArgs
        {
            public GroupAddedEventArg(Group Group)
            {
                this.Group = Group;
            }

            public Group Group { get; }
        }

        public sealed class GroupCollection : List<Group>
        {
            public event EventHandler<GroupAddedEventArg> OnAdd;

            public new Group Add(Group item)
            {
                base.Add(item);
                OnAdd?.Invoke(this, new GroupAddedEventArg(item));
                return item;
            }

            public Group this[string name]
            {
                get
                {
                    Group g = this.First(x => x.Name == name);
                    return g == null ? null : g;
                }
            }
        }

        #endregion Assign the parent reference when group is added to list
    }

    #endregion Layer 1 -- Project

    #region Layer 2 -- Group

    //文档第二层组
    [Serializable]
    public class Group
    {
        #region Private Fields

        private ChannelCollection _channelList;
        private string _name;
        private Project _parent;

        #endregion Private Fields

        #region Public Properties

        [Category("General")]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        [Category("General")]
        public string ID { get; set; }

        [Category("Hardware Setting")]
        public int FrameLength { get; set; }

        [Category("Hardware Setting")]
        public double SampleRate { get; set; }

        [Category("Advance")]
        public string Description { get; set; }

        [Category("General")]
        public ChannelCollection Channels
        {
            get
            {
                return _channelList;
            }
        }

        [Category("General")]
        public Project Project
        {
            get { return _parent; }
        }

        [Category("General")]
        public AnalogWaveformFile FileHandle { get; set; }

        #endregion Public Properties

        internal void ConfigureParent(Project parent)
        {
            _parent = parent;
        }

        #region Constructor

        public Group()
        {
            _channelList = new ChannelCollection();
            _channelList.OnAdd += _channelList_OnAdd;
            SampleRate = 1;
            FrameLength = 1;
            Name = "";
        }

        #endregion Constructor

        public Channel CreateNewChannel(string name)
        {
            var c = new Channel();
            c.Name = name;

            _channelList.Add(c);
            return c;
        }

        #region Assign the parent reference when Channel is added to the list

        public sealed class ChannelAddedEventArg : EventArgs
        {
            public ChannelAddedEventArg(Channel Channel)
            {
                this.Channel = Channel;
            }

            public Channel Channel { get; }
        }

        public sealed class ChannelCollection : List<Channel>
        {
            public event EventHandler<ChannelAddedEventArg> OnAdd;

            public new Channel Add(Channel item)
            {
                this.OnAdd?.Invoke(this, new ChannelAddedEventArg(item));

                base.Add(item);
                return item;
            }

            public Channel this[string name]
            {
                get
                {
                    Channel c = this.First(x => x.Name == name);
                    return c == null ? null : c;
                }
            }
        }

        private void _channelList_OnAdd(object sender, ChannelAddedEventArg e)
        {
            e.Channel.ConfigureParent(this);
        }

        #endregion Assign the parent reference when Channel is added to the list
    }

    #endregion Layer 2 -- Group

    #region Layer 3 -- Channel

    //文档第三层通道
    [Serializable]
    public class Channel
    {
        #region Private Fields

        private Group _parent;

        #endregion Private Fields

        #region Public Properties

        [Category("General")]
        public string Name { get; set; }

        [Category("General")]
        public Group Group
        {
            get { return _parent; }
        }

        [Category("General")]
        public Project Project { get { return _parent.Project; } }

        [Category("Advance")]
        public double Sacle { get; set; } = 1;

        [Category("Advance")]
        public double Offset { get; set; } = 0;

        [Category("Advance")]
        public string Description { get; set; } = "";

        [Category("General")]
        public string Resource { get; set; } = "";

        #endregion Public Properties

        internal void ConfigureParent(Group parent)
        {
            _parent = parent;
        }

        #region Constuctor

        public Channel()
        {
            Name = "";
            Resource = "";
        }

        #endregion Constuctor
    }

    #endregion Layer 3 -- Channel
}