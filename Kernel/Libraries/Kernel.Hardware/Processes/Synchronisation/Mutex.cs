﻿#region LICENSE
// ---------------------------------- LICENSE ---------------------------------- //
//
//    Fling OS - The educational operating system
//    Copyright (C) 2015 Edward Nutting
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//  Project owner: 
//		Email: edwardnutting@outlook.com
//		For paper mail address, please contact via email for details.
//
// ------------------------------------------------------------------------------ //
#endregion
    
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kernel.Hardware.Processes.Synchronisation
{
    public class Mutex : FOS_System.Object
    {
        protected int id;
        public int Id
        {
            get
            {
                return id;
            }
        }

        private bool locked = false;
        public bool Locked
        {
            get
            {
                return locked;
            }
        }
        private bool padding = false;

        public Mutex(int anId)
        {
            id = anId;
        }

        [Compiler.PluggedMethod(ASMFilePath=@"ASM\Processes\Synchronisation\Mutex")]
        [Drivers.Compiler.Attributes.PluggedMethod(ASMFilePath=@"ASM\Processes\Synchronisation\Mutex")]
        public void Enter()
        {
        }
        [Compiler.PluggedMethod(ASMFilePath=null)]
        [Drivers.Compiler.Attributes.PluggedMethod(ASMFilePath=null)]
        public void Exit()
        {
        }
    }
}
