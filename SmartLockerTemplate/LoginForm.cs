using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartLockerTemplate
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string id = this.idTextBox.Text;
            string pw = this.pwTextBox.Text;
            
            MessageBox.Show("ID: " + id + ", PW: " + pw, "로그인",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
