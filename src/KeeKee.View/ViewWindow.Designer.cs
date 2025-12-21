// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace KeeKee.View {
    partial class ViewWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Override class that wraps Panel and makes sure it doesn't get
        /// painted so the OnPaint operation only is the callback taht we
        /// registered in the regular RadegastWindow class.
        /// </summary>
        private class LGPanel : System.Windows.Forms.Panel {
            public LGPanel()
                : base() {
                // DoubleBuffered = true;
                SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(System.Windows.Forms.ControlStyles.UserPaint, true);
                // SetStyle(System.Windows.Forms.ControlStyles.Opaque, true);
            }
            protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e) {
                // base.OnPaintBackground(e);
            }
            
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewWindow));
            this.LGWindow = new KeeKee.View.ViewWindow.LGPanel();
            this.SuspendLayout();
            // 
            // LGWindow
            // 
            this.LGWindow.Location = new System.Drawing.Point(4, 4);
            this.LGWindow.Name = "LGWindow";
            this.LGWindow.Size = new System.Drawing.Size(800, 600);
            this.LGWindow.TabIndex = 0;
            this.LGWindow.MouseLeave += new System.EventHandler(this.LGWindow_MouseLeave);
            this.LGWindow.Paint += new System.Windows.Forms.PaintEventHandler(this.LGWindow_Paint);
            this.LGWindow.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseMove);
            this.LGWindow.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseDown);
            this.LGWindow.Resize += new System.EventHandler(this.LGWindow_Resize);
            this.LGWindow.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseUp);
            this.LGWindow.MouseEnter += new System.EventHandler(this.LGWindow_MouseEnter);
            // 
            // ViewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 608);
            this.Controls.Add(this.LGWindow);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ViewWindow";
            this.Text = "KeeKee -- World";
            this.Load += new System.EventHandler(this.ViewWindow_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LGWindow_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LGWindow_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private ViewWindow.LGPanel LGWindow;
    }
}
