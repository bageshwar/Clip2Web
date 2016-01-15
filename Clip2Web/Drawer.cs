using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clip2Web
{
    public class Drawer : Form
    {
        private NotifyIcon m_trayIcon;
        private ContextMenu m_trayMenu;

        private IntPtr m_clipboardViewerNext;

        private string m_tempFile = "";

        private Bitmap m_imageData;


        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;

        private const string INTERESTING_FORMAT = "System.Drawing.Bitmap";

        public Drawer()
        {
            // Create a simple tray menu with only one item.
            m_trayMenu = new ContextMenu();
            m_trayMenu.MenuItems.Add("Exit", OnExit);
            //m_trayMenu.MenuItems.Add("Configure", Configure);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            m_trayIcon = new NotifyIcon();
            m_trayIcon.Text = "Clip2Web";
            //m_trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            m_trayIcon.Icon = Clip2Web.Resources.Icon;

            // Add menu to tray icon and show it.
            m_trayIcon.ContextMenu = m_trayMenu;
            m_trayIcon.BalloonTipClicked += TipClicked;
            m_trayIcon.Visible = true;
        }

        private void TipClicked(object sender, EventArgs e)
        {
            DoTheStuff();
        }

        private void DoTheStuff()
        {
            m_tempFile = Path.GetTempFileName() + ".png";
            m_imageData.Save(m_tempFile);
            Clipboard.SetText(m_tempFile);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                //
                // The WM_DRAWCLIPBOARD message is sent to the first window 
                // in the clipboard viewer chain when the content of the 
                // clipboard changes. This enables a clipboard viewer 
                // window to display the new content of the clipboard. 
                //
                case WM_DRAWCLIPBOARD:
                    
                    GetClipboardData();

                    //
                    // Each window that receives the WM_DRAWCLIPBOARD message 
                    // must call the SendMessage function to pass the message 
                    // on to the next window in the clipboard viewer chain.
                    //
                    User32.SendMessage(m_clipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    break;


                //
                // The WM_CHANGECBCHAIN message is sent to the first window 
                // in the clipboard viewer chain when a window is being 
                // removed from the chain. 
                //
                case WM_CHANGECBCHAIN:
                    Debug.WriteLine("WM_CHANGECBCHAIN: lParam: " + m.LParam, "WndProc");

                    // When a clipboard viewer window receives the WM_CHANGECBCHAIN message, 
                    // it should call the SendMessage function to pass the message to the 
                    // next window in the chain, unless the next window is the window 
                    // being removed. In this case, the clipboard viewer should save 
                    // the handle specified by the lParam parameter as the next window in the chain. 

                    //
                    // wParam is the Handle to the window being removed from 
                    // the clipboard viewer chain 
                    // lParam is the Handle to the next window in the chain 
                    // following the window being removed. 
                    if (m.WParam == m_clipboardViewerNext)
                    {
                        //
                        // If wParam is the next clipboard viewer then it
                        // is being removed so update pointer to the next
                        // window in the clipboard chain
                        //
                        m_clipboardViewerNext = m.LParam;
                    }
                    else
                    {
                        User32.SendMessage(m_clipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    }
                    break;

                default:
                    //
                    // Let the form process the messages that we are
                    // not interested in
                    //
                    base.WndProc(ref m);
                    break;

            }

        }

        private void GetClipboardData()
        {
            //
            // Data on the clipboard uses the 
            // IDataObject interface
            //
            IDataObject dataObject = new DataObject();
            
            try
            {
                dataObject = Clipboard.GetDataObject();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Clip2Web Error: " + ex.ToString());
                return;
            }

            var formats = dataObject.GetFormats();
            
            if(formats.Contains(INTERESTING_FORMAT) || dataObject.GetDataPresent(DataFormats.Bitmap))
            {
                m_imageData = (System.Drawing.Bitmap)dataObject.GetData(DataFormats.Bitmap);
                m_trayIcon.ShowBalloonTip(1000, "Clip Saved!", "Click to copy path", ToolTipIcon.Info);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            m_clipboardViewerNext = User32.SetClipboardViewer(this.Handle);
            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Configure(object sender, EventArgs e)
        {
            // Show the configurator form.
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                
                User32.ChangeClipboardChain(this.Handle, m_clipboardViewerNext);
                // Release the icon resource.
                m_trayIcon.Dispose();

            }

            base.Dispose(isDisposing);
        }
        
    }
}
