using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {
        Queue<string> files = new Queue<string>();
        bool isProcessing = false;
        public Form1()
        {
            InitializeComponent();
            listBox1.TopIndex = 0;
            if (Program.global_path != null)
                textBox1.Text = Program.global_path;
            if (Program.global_password != null)
                textBox2.Text = Program.global_password;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread A;
            if (File.Exists(textBox1.Text))
                A = new Thread(new ThreadStart(() => encryptsingle(textBox1.Text)));
            else if(Directory.Exists(textBox1.Text))
                A = new Thread(encryptfiles);
            else
            {
                MessageBox.Show("文件或文件夹不存在！");
                return;
            }
            listBox1.Items.Clear();
            A.IsBackground = true;
            A.Start();
        }
        void encryptfiles()
        {
            string targetdir = "";
            if (!Directory.Exists(textBox1.Text))
            { MessageBox.Show("文件夹不存在！"); return; }
            if (textBox2.Text == null || textBox2.Text == "")
            { MessageBox.Show("密码不能为空！"); return; }
            string[] _files = Directory.GetFiles(textBox1.Text, "*.*", SearchOption.AllDirectories);
            foreach (string i in _files)
                files.Enqueue(i);
            int total = files.Count, progress = 0;
            this.Invoke(new Action(delegate
            {
                this.progressBar1.Maximum = total;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.label3.Text = "处理中……";
            }));
            isProcessing = true;
            DirectoryInfo temp = new DirectoryInfo(textBox1.Text);
            targetdir = Path.Combine(temp.Parent.FullName, temp.Name + "_Encrypted");
            Directory.CreateDirectory(targetdir);
            List<Thread> workingthreads = new List<Thread>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
                workingthreads.Add(new Thread(new ThreadStart(() => encrypt(ref files, targetdir, ref progress, total))));
            foreach (Thread i in workingthreads)
                i.Start();
            foreach (Thread i in workingthreads)
            { i.Join(); }
            this.Invoke(new Action(delegate
            {
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.label3.Text = "完成！";
            }));
            isProcessing = false;
            MessageBox.Show("加密完成！路径为：" + targetdir);
        }
        void encrypt(ref Queue<string> files, string targetdir, ref int progress, int size)
        {
            AesEncryption encryption = new AesEncryption("cfb");
            if (files.Count == 0)
                return;
            string file = files.Dequeue();
            while (file != null || file != "")
            {
                if (file == null)
                    return;
                string targetfile = Path.Combine(targetdir, GetRelativePath(textBox1.Text + "\\", file));
                FileInfo temp = new FileInfo(targetfile);
                if (!temp.Directory.Exists)
                    temp.Directory.Create();
                string msg;
                if (temp.Extension != ".enc")
                    msg = encryption.EncryptFile(file, targetfile, textBox2.Text);
                else
                    msg = null;
                if (msg != null)
                    this.Invoke(new Action(() => this.listBox1.Items.Add("已加密：" + file + "\n")));
                else
                    this.Invoke(new Action(() => this.listBox1.Items.Add("加密失败：" + file + "\n")));
                int prog = ++progress;
                this.Invoke(new Action(delegate
                {
                    this.progressBar1.Value = prog;
                    this.label3.Text = String.Format("正在处理第{0}个文件，共{1}个文件\n路径:{2}", prog, size, targetfile);
                    this.listBox1.TopIndex = this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight)+2;
                }));
                file = null;
                if (files.Count == 0)
                    return;
                file = files.Dequeue();
            }
        }
        void decryptfiles()
        {
            string targetdir = "";
            if (!Directory.Exists(textBox1.Text))
            { MessageBox.Show("文件夹不存在！"); return; }
            if (textBox2.Text == null || textBox2.Text == "")
            { MessageBox.Show("密码不能为空！"); return; }
            string[] _files = Directory.GetFiles(textBox1.Text, "*.*", SearchOption.AllDirectories);
            foreach (string i in _files)
                files.Enqueue(i);
            int total = files.Count, progress = 0;
            isProcessing = true;
            this.Invoke(new Action(delegate
            {
                this.progressBar1.Maximum = total;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.label3.Text = "处理中……";
            }));
            DirectoryInfo temp = new DirectoryInfo(textBox1.Text);
            targetdir = Path.Combine(temp.Parent.FullName, Regex.Replace(temp.Name, "_Encrypted", "_Decrypted"));
            Directory.CreateDirectory(targetdir);
            List<Thread> workingthreads = new List<Thread>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
                workingthreads.Add(new Thread(new ThreadStart(() => decrypt(ref files, targetdir, ref progress, total))));
            foreach (Thread i in workingthreads)
                i.Start();
            foreach (Thread i in workingthreads)
            { i.Join(); }
            this.Invoke(new Action(delegate
            {
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.label3.Text = "完成！";
            }));
            isProcessing = false;
            MessageBox.Show("解密完成！路径为：" + targetdir);
        }
        void decrypt(ref Queue<string> files, string targetdir, ref int progress, int size)
        {
            AesEncryption encryption = new AesEncryption("cfb");
            if (files.Count == 0)
                return;
            string file = files.Dequeue();
            while (file != null || file != "")
            {
                if (file == null)
                    return;
                string targetfile = Path.Combine(targetdir, GetRelativePath(textBox1.Text + "\\", file));
                FileInfo temp = new FileInfo(targetfile);
                if (!temp.Directory.Exists)
                    temp.Directory.Create();
                string msg = encryption.DecryptFile(file, targetfile, textBox2.Text);
                if (msg != null)
                    this.Invoke(new Action(() => this.listBox1.Items.Add("已解密：" + file + "\n")));
                else
                    this.Invoke(new Action(() => this.listBox1.Items.Add("解密失败：" + file + "\n")));
                int prog = ++progress;
                this.Invoke(new Action(delegate
                {
                    this.progressBar1.Value = prog;
                    this.label3.Text = String.Format("正在处理第{0}个文件，共{1}个文件\n路径:{2}", prog, size, targetfile);
                    this.listBox1.TopIndex = this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight)+2;
                }));
                file = null;
                if (files.Count == 0)
                    return;
                file = files.Dequeue();
            }
        }
        void encryptsingle(string file)
        {
            if (textBox2.Text == null || textBox2.Text == "")
            { MessageBox.Show("密码不能为空！"); return; }
            this.Invoke(new Action(delegate
            {
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.label3.Text = String.Format("正在处理:{0}", file);
                this.progressBar1.Value = this.progressBar1.Minimum;
            }));
            AesEncryption encryption = new AesEncryption("cfb");
            if (file == null)
                return;
            FileInfo temp = new FileInfo(file);
            string msg;
            if (temp.Extension != ".enc")
                msg = encryption.EncryptFile(file, file, textBox2.Text);
            else
                msg = null;
            if (msg != null)
                this.Invoke(new Action(() => this.listBox1.Items.Add("已加密：" + file + "\n")));
            else
                this.Invoke(new Action(() => this.listBox1.Items.Add("加密失败：" + file + "\n")));
            this.Invoke(new Action(delegate
            {
                this.progressBar1.Value = this.progressBar1.Maximum;
                this.listBox1.TopIndex = this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight) + 2;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.label3.Text = String.Format("完成！");
            }));
        }
        void decryptsingle(string file)
        {
            if (textBox2.Text == null || textBox2.Text == "")
            { MessageBox.Show("密码不能为空！"); return; }
            this.Invoke(new Action(delegate
            {
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.label3.Text = String.Format("正在处理:{0}", file);
                this.progressBar1.Value = this.progressBar1.Minimum;
            }));
            AesEncryption encryption = new AesEncryption("cfb");
            if (file == null)
                return;
            FileInfo temp = new FileInfo(file);
            string msg;
            if (temp.Extension == ".enc")
                msg = encryption.DecryptFile(file,file, textBox2.Text);
            else
                msg = null;
            if (msg != null)
                this.Invoke(new Action(() => this.listBox1.Items.Add("已解密：" + file + "\n")));
            else
                this.Invoke(new Action(() => this.listBox1.Items.Add("解密失败：" + file + "\n")));
            this.Invoke(new Action(delegate
            {
                this.progressBar1.Value = this.progressBar1.Maximum;
                this.listBox1.TopIndex = this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight) + 2;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.label3.Text = String.Format("完成！");
            }));
        }
        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            textBox1.Text = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }
        public string GetRelativePath(string strPath1, string strPath2)
        {
            if (!strPath1.EndsWith("//")) strPath1 += "//";    //如果不是以"/"结尾的加上"/"
            int intIndex = -1, intPos = strPath1.IndexOf('\\');
            ///以"/"为分界比较从开始处到第一个"/"处对两个地址进行比较,如果相同则扩展到
            ///下一个"/"处;直到比较出不同或第一个地址的结尾.
            while (intPos >= 0)
            {
                intPos++;
                if (string.Compare(strPath1, 0, strPath2, 0, intPos, true) != 0) break;
                intIndex = intPos;
                intPos = strPath1.IndexOf('\\', intPos);
            }

            ///如果从不是第一个"/"处开始有不同,则从最后一个发现有不同的"/"处开始将strPath2
            ///的后面部分付值给自己,在strPath1的同一个位置开始望后计算每有一个"/"则在strPath2
            ///的前面加上一个"../"(经过转义后就是"..//").
            if (intIndex >= 0)
            {
                strPath2 = strPath2.Substring(intIndex);
                intPos = strPath1.IndexOf("\\", intIndex);
                while (intPos >= 0)
                {
                    strPath2 = "\\" + strPath2;
                    intPos = strPath1.IndexOf("\\", intPos + 1);
                }
            }
            //否则直接返回strPath2
            return strPath2;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread A;
            if (File.Exists(textBox1.Text))
                A = new Thread(new ThreadStart(() => decryptsingle(textBox1.Text)));
            else if (Directory.Exists(textBox1.Text))
                A = new Thread(decryptfiles);
            else
            {
                MessageBox.Show("文件或文件夹不存在！");
                return;
            }
            listBox1.Items.Clear();
            A.IsBackground = true;
            A.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessing)
            {
                DialogResult dr;
                dr = MessageBox.Show("文件正在处理中！确定要退出吗？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if(dr == DialogResult.OK)
                    Application.ExitThread();
                else
                    e.Cancel = true;
            }
            else
                Application.ExitThread();
        }
    }
}
