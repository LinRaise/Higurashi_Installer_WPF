﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Higurashi_Installer_WPF
{
    /// <summary>
    /// Interaction logic for DebugConsole.xaml
    /// 
    /// The debug console consists of the "current line" and "previous lines"
    /// This is done just to make processing the Aria2C output easier.
    /// The current line is copied to the previous lines buffer, unless the 
    /// current line is:
    ///  - a temporary progress update from Aria2C (only the last temp update will be printed)
    ///  - a all-spaces blank line from Aria2C
    ///  Note also that any color codes (using the ASCII "ESC" character 
    ///  followed by the color code) are not used or interpreted, so you'll
    ///  see some garbage in the output whenever color codes are used.
    /// </summary>
    public partial class DebugConsole : Window
    {
        //Match "[#abc3b6" or similar (hex number always differs)
        //these sorts of lines are considered "temporary" as they are only used by Aria2C to report progress
        Regex Aria2CTempOutputRegex = new Regex(@"^\[#[\w]+ ",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //Match a line consisting of ONLY 40 or more spaces. Treat it as a aria2c blank line
        Regex Aria2CBlankLineRegex = new Regex(@"^ {40,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);        

        public DebugConsole()
        {
            InitializeComponent();
        }

        public void Println(string nextLine)
        {
            //ignore blank lines (TODO: only strip lines which are all spaces)
            if (Aria2CBlankLineRegex.IsMatch(nextLine))
                return;

            //append current line to previous lines under the following two cases: 
            // - the current line is not a temporary aria2c line
            // - OR the next line is not a temporary aria2c line (this ensures 
            //   at least one temporary update is added to previous lines buffer)
            if (!Aria2CTempOutputRegex.IsMatch(DebugConsoleCurrentLine.Text) ||
                !Aria2CTempOutputRegex.IsMatch(nextLine))
            {
                DebugConsolePreviousLines.AppendText(DebugConsoleCurrentLine.Text);
                DebugConsolePreviousLines.AppendText(Environment.NewLine);
            }
            
            //set the current line
            DebugConsoleCurrentLine.Text = nextLine;
        }

        //If the user clicks the 'X' to close the window, just hide the window instead of closing it.
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}