﻿using Kernel.Consoles;
using Kernel.Devices;
using Kernel.Framework.Processes;
using Kernel.PCI;
using Kernel.Utilities;
using Kernel.VGA.VMWare;

namespace Kernel.Tasks.Driver
{
    public static class PCIDriverTask
    {
        private static VirtualConsole console;

        private static uint GCThreadId;

        private static bool Terminating = false;

        public static unsafe void Main()
        {
            Helpers.ProcessInit("PCI Driver", out GCThreadId);

            try
            {
                BasicConsole.WriteLine("PCI Driver > Creating virtual console...");
                console = new VirtualConsole();

                BasicConsole.WriteLine("PCI Driver > Connecting virtual console...");
                console.Connect();

                BasicConsole.WriteLine("PCI Driver > Executing.");

                DeviceManager.InitForProcess();

                try
                {
                    BasicConsole.WriteLine("PCI Driver > Initialising PCI Manager...");
                    PCIManager.Init();

                    BasicConsole.WriteLine("PCI Driver > Enumerating PCI devices...");
                    PCIManager.EnumerateDevices();

                    BasicConsole.WriteLine("PCI Driver > Starting accessors thread...");
                    PCIManager.StartAccessorsThread();

                    //BasicConsole.WriteLine("PCI Driver > Outputting PCI info...");
                    //OutputPCI();

                    SVGAII svga = null;
                    for (int i = 0; i < PCIManager.Devices.Count; i++)
                    {
                        PCIDevice aDevice = (PCIDevice)PCIManager.Devices[i];
                        if (aDevice.VendorID == SVGAII_Registers.PCI_VENDOR_ID && 
                            aDevice.DeviceID == SVGAII_Registers.PCI_DEVICE_ID)
                        {
                            BasicConsole.WriteLine("PCI Driver > Found an VMWare SVGA-II...");
                            svga = new SVGAII((PCIDeviceNormal)aDevice);
                            break;
                        }
                    }

                    if (svga != null)
                    {
                        GMR gmr = new GMR();
                        gmr.Init(svga);
                        
                        svga.SetMode(0, 0, 32);
                        
                        Screen.Init(svga);
                        
                        SVGAII_Registers.ScreenObject screenObject = new SVGAII_Registers.ScreenObject()
                        {
                            StructSize = (uint)sizeof(SVGAII_Registers.ScreenObject),
                            Id = 0,
                            Flags = (uint)(SVGAII_Registers.Screen.HAS_ROOT | SVGAII_Registers.Screen.IS_PRIMARY),
                            Size = new SVGAII_Registers.UnsignedDimensions()
                            {
                                Width = 1920,
                                Height = 1080
                            },
                            Root = new SVGAII_Registers.SignedPoint()
                            {
                                X = -500,
                                Y = 10000
                            }
                        };
                        
                        Screen.Create(svga, &screenObject);
                        
                        uint gmrId = 0;
                        byte bitsPerPixel = 32;
                        byte colourDepth = 24;

                        uint bytesPerPixel = (uint)bitsPerPixel >> 3;
                        uint fbBytesPerLine = screenObject.Size.Width * bytesPerPixel;
                        uint fbSizeInBytes = fbBytesPerLine * screenObject.Size.Height;
                        uint fbSizeInPages = (fbSizeInBytes + GMR.PAGE_MASK) / GMR.PAGE_SIZE;

                        uint fbFirstPage = gmr.DefineContiguous(svga, gmrId, fbSizeInPages);
                        uint* fbPointer = (uint*)GMR.PPN_POINTER(fbFirstPage);

                        SVGAII_Registers.GuestPointer fbGuestPtr = new SVGAII_Registers.GuestPointer()
                        {
                            GMRId = gmrId,
                            Offset = 0
                        };

                        SVGAII_Registers.GMRImageFormat fbFormat = new SVGAII_Registers.GMRImageFormat()
                        {
                            BitsPerPixel = bitsPerPixel,
                            ColourDepth = colourDepth
                        };
                        
                        Screen.DefineGMRFB(svga, fbGuestPtr, fbBytesPerLine, fbFormat);

                        SVGAII_Registers.SignedPoint blitOrigin = new SVGAII_Registers.SignedPoint()
                        {
                            X = 0,
                            Y = 0
                        };

                        SVGAII_Registers.SignedRectangle blitDest = new SVGAII_Registers.SignedRectangle()
                        {
                            Left = 0,
                            Top = 0,
                            Right = (int)screenObject.Size.Width,
                            Bottom = (int)screenObject.Size.Height
                        };

                        byte* cPtr = (byte*)fbPointer;
                        uint GreyLevels = 50;
                        uint Height = 50;
                        uint linesPerGreyIncrease = Height / GreyLevels;
                        uint bytesPerBand = fbBytesPerLine * linesPerGreyIncrease;
                        for (uint i = 0; i < GreyLevels; i++, cPtr += bytesPerBand)
                        {
                            MemoryUtils.MemSet(cPtr, (byte)i, bytesPerBand);
                        }

                        uint GreyLevels2 = 256 - GreyLevels;
                        Height = screenObject.Size.Height - Height;
                        linesPerGreyIncrease = Height / GreyLevels2;
                        bytesPerBand = fbBytesPerLine * linesPerGreyIncrease;
                        uint end = GreyLevels2 + GreyLevels;
                        for (uint i = GreyLevels; i < end; i++, cPtr += bytesPerBand)
                        {
                            MemoryUtils.MemSet(cPtr, (byte)i, bytesPerBand);
                        }

                        Screen.BlitFromGMRFB(svga, &blitOrigin, &blitDest, screenObject.Id);
                        
                        uint dmaFence = svga.InsertFence();
                        
                        svga.SyncToFence(dmaFence);
                        
                        MemoryUtils.MemSet((byte*)fbPointer, 0x42, fbSizeInBytes);
                    }

                    //AMDPCNetII pcnet = null;
                    //for (int i = 0; i < PCIManager.Devices.Count; i++)
                    //{
                    //    PCIDevice aDevice = (PCIDevice)PCIManager.Devices[i];
                    //    if (aDevice.VendorID == 0x1022 && aDevice.DeviceID == 0x2000)
                    //    {
                    //        pcnet = new AMDPCNetII(aDevice.bus, aDevice.slot, aDevice.function);
                    //        BasicConsole.WriteLine("PCI Driver > Found an AMDPCNetII...");
                    //        break;
                    //    }
                    //}

                    //if (pcnet != null)
                    //{
                    //    if (pcnet.Init())
                    //      pcnet.Start();

                    //    while (true) { };
                    //}

                }
                catch
                {
                    BasicConsole.WriteLine("PCI Driver > Error executing!");
                    BasicConsole.WriteLine(ExceptionMethods.CurrentException.Message);
                }

                BasicConsole.WriteLine("PCI Driver > Execution complete.");
            }
            catch
            {
                BasicConsole.WriteLine("PCI Driver > Error initialising!");
                BasicConsole.WriteLine(ExceptionMethods.CurrentException.Message);
            }

            BasicConsole.WriteLine("PCI Driver > Exiting...");
        }

        /// <summary>
        ///     Outputs the PCI system information.
        /// </summary>
        private static void OutputPCI()
        {
            for (int i = 0; i < PCIManager.Devices.Count; i++)
            {
                PCIDevice aDevice = (PCIDevice)PCIManager.Devices[i];
                console.WriteLine(PCIDevice.DeviceClassInfo.GetString(aDevice));
                console.Write(" - Address: ");
                console.Write(aDevice.bus);
                console.Write(":");
                console.Write(aDevice.slot);
                console.Write(":");
                console.WriteLine(aDevice.function);

                console.Write(" - Vendor Id: ");
                console.WriteLine(aDevice.VendorID);

                console.Write(" - Device Id: ");
                console.WriteLine(aDevice.DeviceID);
            }
        }
    }
}