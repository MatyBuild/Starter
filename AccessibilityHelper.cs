using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ButtonRecognitionTool
{
    public class AccessibilityHelper
    {
        [DllImport("oleacc.dll")]
        public static extern int AccessibleObjectFromWindow(
            IntPtr hwnd,
            uint dwObjectID,
            byte[] riid,
            ref IntPtr ptr);

        [DllImport("oleacc.dll")]
        public static extern int AccessibleChildren(
            IntPtr paccContainer,
            int iChildStart,
            int cChildren,
            [Out] object[] rgvarChildren,
            out int pcObtained);

        [ComImport]
        [Guid("618736E0-3C3D-11CF-810C-00AA00389B71")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAccessible
        {
            void accDoDefaultAction([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object accHitTest(int xLeft, int yTop);
            void accLocation(out int pxLeft, out int pyTop, out int pcxWidth, out int pcyHeight, [In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object accNavigate(int navDir, [In, MarshalAs(UnmanagedType.Variant)] object varStart);
            void accSelect(int flagsSelect, [In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object get_accChild([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            int get_accChildCount();
            string get_accDefaultAction([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            string get_accDescription([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object get_accFocus();
            string get_accHelp([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            int get_accHelpTopic(out string pszHelpFile, [In, MarshalAs(UnmanagedType.Variant)] object varChild);
            string get_accKeyboardShortcut([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            string get_accName([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object get_accParent();
            object get_accRole([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            object get_accSelection();
            object get_accState([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            string get_accValue([In, MarshalAs(UnmanagedType.Variant)] object varChild);
            void set_accName([In, MarshalAs(UnmanagedType.Variant)] object varChild, string szName);
            void set_accValue([In, MarshalAs(UnmanagedType.Variant)] object varChild, string pszValue);
        }

        private const uint OBJID_WINDOW = 0x00000000;
        private const uint OBJID_CLIENT = 0xFFFFFFFC;
        private const int ROLE_SYSTEM_PUSHBUTTON = 43;

        public List<ButtonInfo> FindButtonsUsingAccessibility(IntPtr windowHandle, bool debugMode = false)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();

            try
            {
                if (debugMode)
                    Console.WriteLine("Trying Accessibility API...");

                // Get IAccessible interface for the window
                IntPtr accessible = IntPtr.Zero;
                byte[] iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71").ToByteArray();
                
                int result = AccessibleObjectFromWindow(windowHandle, OBJID_CLIENT, iid, ref accessible);
                if (result != 0 || accessible == IntPtr.Zero)
                {
                    if (debugMode)
                        Console.WriteLine($"AccessibleObjectFromWindow failed with result: {result}");
                    return buttons;
                }

                var accessibleObj = Marshal.GetObjectForIUnknown(accessible) as IAccessible;
                if (accessibleObj == null)
                {
                    if (debugMode)
                        Console.WriteLine("Could not get IAccessible interface");
                    return buttons;
                }

                if (debugMode)
                    Console.WriteLine("Successfully got IAccessible interface, searching for buttons...");

                // Recursively search for buttons
                SearchForButtons(accessibleObj, buttons, debugMode, 0);

                Marshal.ReleaseComObject(accessibleObj);
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine($"Accessibility API error: {ex.Message}");
            }

            return buttons;
        }

        private void SearchForButtons(IAccessible accessible, List<ButtonInfo> buttons, bool debugMode, int depth)
        {
            if (depth > 10) return; // Prevent infinite recursion

            try
            {
                int childCount = accessible.get_accChildCount();
                if (debugMode && childCount > 0)
                    Console.WriteLine($"{"".PadLeft(depth * 2)}Found {childCount} children at depth {depth}");

                if (childCount == 0) return;

                object[] children = new object[childCount];
                int obtained = 0;
                int result = AccessibleChildren(Marshal.GetIUnknownForObject(accessible), 0, childCount, children, out obtained);

                if (result == 0 && obtained > 0)
                {
                    for (int i = 0; i < obtained; i++)
                    {
                        try
                        {
                            // Check if this child is a button
                            var role = accessible.get_accRole(children[i]);
                            if (role != null && (int)role == ROLE_SYSTEM_PUSHBUTTON)
                            {
                                // This is a button!
                                string name = "";
                                string description = "";
                                try
                                {
                                    name = accessible.get_accName(children[i]) ?? "";
                                    description = accessible.get_accDescription(children[i]) ?? "";
                                }
                                catch { }

                                if (debugMode)
                                    Console.WriteLine($"{"".PadLeft(depth * 2)}Found BUTTON: '{name}' - '{description}'");

                                // Create ButtonInfo (we don't have HWND for accessibility objects)
                                var buttonInfo = new ButtonInfo
                                {
                                    Handle = IntPtr.Zero, // Accessibility objects don't have HWNDs
                                    Text = name,
                                    ClassName = "AccessibleButton",
                                    IsEnabled = true, // We'd need to check state properly
                                    IsVisible = true,
                                    ControlId = i
                                };

                                buttons.Add(buttonInfo);
                            }

                            // If the child is an IAccessible object, search it recursively
                            if (children[i] is IAccessible childAccessible)
                            {
                                SearchForButtons(childAccessible, buttons, debugMode, depth + 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (debugMode)
                                Console.WriteLine($"{"".PadLeft(depth * 2)}Error processing child {i}: {ex.Message}");
                        }
                    }
                }
                else if (debugMode)
                {
                    Console.WriteLine($"{"".PadLeft(depth * 2)}AccessibleChildren failed or returned 0 items");
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine($"{"".PadLeft(depth * 2)}Error in SearchForButtons: {ex.Message}");
            }
        }
    }
}