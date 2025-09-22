using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ButtonRecognitionTool
{
    public class UIAutomationHelper
    {
        // UI Automation COM interfaces and constants
        [ComImport]
        [Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomation
        {
            IntPtr CompareElements(IntPtr el1, IntPtr el2);
            IntPtr CompareRuntimeIds(IntPtr runtimeId1, IntPtr runtimeId2);
            IntPtr GetRootElement();
            IntPtr ElementFromHandle(IntPtr hwnd);
            IntPtr ElementFromPoint(POINT pt);
            IntPtr GetFocusedElement();
            IntPtr GetRootElementBuildCache(IntPtr cacheRequest);
            IntPtr ElementFromHandleBuildCache(IntPtr hwnd, IntPtr cacheRequest);
            IntPtr ElementFromPointBuildCache(POINT pt, IntPtr cacheRequest);
            IntPtr GetFocusedElementBuildCache(IntPtr cacheRequest);
            IntPtr CreateTreeWalker(IntPtr pCondition);
            IntPtr ControlViewWalker { get; }
            IntPtr ContentViewWalker { get; }
            IntPtr RawViewWalker { get; }
            IntPtr RawViewCondition { get; }
            IntPtr ControlViewCondition { get; }
            IntPtr ContentViewCondition { get; }
            IntPtr CreateCacheRequest();
            IntPtr CreateTrueCondition();
            IntPtr CreateFalseCondition();
            IntPtr CreatePropertyCondition(int propertyId, object value);
            IntPtr CreatePropertyConditionEx(int propertyId, object value, int flags);
            IntPtr CreateAndCondition(IntPtr condition1, IntPtr condition2);
            IntPtr CreateAndConditionFromArray(IntPtr conditions);
            IntPtr CreateOrCondition(IntPtr condition1, IntPtr condition2);
            IntPtr CreateOrConditionFromArray(IntPtr conditions);
            IntPtr CreateNotCondition(IntPtr condition);
            IntPtr AddAutomationEventHandler(int eventId, IntPtr element, int scope, IntPtr cacheRequest, IntPtr handler);
            IntPtr RemoveAutomationEventHandler(int eventId, IntPtr element, IntPtr handler);
            IntPtr AddAutomationFocusChangedEventHandler(IntPtr cacheRequest, IntPtr handler);
            IntPtr RemoveAutomationFocusChangedEventHandler(IntPtr handler);
            IntPtr RemoveAllEventHandlers();
            IntPtr IntNativeArrayToSafeArray(IntPtr array, int arrayCount);
            IntPtr IntSafeArrayToNativeArray(IntPtr intArray);
            IntPtr RectToVariant(RECT rc);
            IntPtr VariantToRect(object var);
            IntPtr SafeArrayToRectNativeArray(IntPtr rects);
            IntPtr CreateProxyFactoryEntry(IntPtr factory);
            IntPtr ProxyFactoryMapping { get; }
            IntPtr GetPropertyProgrammaticName(int property);
            IntPtr GetPatternProgrammaticName(int pattern);
            IntPtr PollForPotentialSupportedPatterns(IntPtr pElement, out IntPtr patternIds, out IntPtr patternNames);
            IntPtr PollForPotentialSupportedProperties(IntPtr pElement, out IntPtr propertyIds, out IntPtr propertyNames);
            IntPtr CheckNotSupported(object value);
            IntPtr ReservedNotSupportedValue { get; }
            IntPtr ReservedMixedAttributeValue { get; }
            IntPtr ElementFromIAccessible(IntPtr accessible, int childId);
            IntPtr ElementFromIAccessibleBuildCache(IntPtr accessible, int childId, IntPtr cacheRequest);
        }

        [ComImport]
        [Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationElement
        {
            IntPtr SetFocus();
            IntPtr GetRuntimeId(out IntPtr runtimeId);
            IntPtr FindFirst(int scope, IntPtr condition);
            IntPtr FindAll(int scope, IntPtr condition);
            IntPtr FindFirstBuildCache(int scope, IntPtr condition, IntPtr cacheRequest);
            IntPtr FindAllBuildCache(int scope, IntPtr condition, IntPtr cacheRequest);
            IntPtr BuildUpdatedCache(IntPtr cacheRequest);
            IntPtr GetCurrentPropertyValue(int propertyId);
            IntPtr GetCurrentPropertyValueEx(int propertyId, bool ignoreDefaultValue);
            IntPtr GetCachedPropertyValue(int propertyId);
            IntPtr GetCachedPropertyValueEx(int propertyId, bool ignoreDefaultValue);
            IntPtr GetCurrentPatternAs(int patternId, ref Guid riid);
            IntPtr GetCachedPatternAs(int patternId, ref Guid riid);
            IntPtr GetCurrentPattern(int patternId);
            IntPtr GetCachedPattern(int patternId);
            IntPtr GetCachedParent();
            IntPtr GetCachedChildren();
            int CurrentProcessId { get; }
            int CurrentControlType { get; }
            string CurrentLocalizedControlType { get; }
            string CurrentName { get; }
            string CurrentAcceleratorKey { get; }
            string CurrentAccessKey { get; }
            bool CurrentHasKeyboardFocus { get; }
            bool CurrentIsKeyboardFocusable { get; }
            bool CurrentIsEnabled { get; }
            string CurrentAutomationId { get; }
            string CurrentClassName { get; }
            string CurrentHelpText { get; }
            int CurrentCulture { get; }
            bool CurrentIsControlElement { get; }
            bool CurrentIsContentElement { get; }
            string CurrentProviderDescription { get; }
            bool CurrentIsOffscreen { get; }
            int CurrentOrientation { get; }
            string CurrentFrameworkId { get; }
            bool CurrentIsRequiredForForm { get; }
            string CurrentItemStatus { get; }
            RECT CurrentBoundingRectangle { get; }
            IntPtr CurrentLabeledBy { get; }
            string CurrentAriaRole { get; }
            string CurrentAriaProperties { get; }
            bool CurrentIsDataValidForForm { get; }
            IntPtr CurrentControllerFor { get; }
            IntPtr CurrentDescribedBy { get; }
            IntPtr CurrentFlowsTo { get; }
        }

        [ComImport]
        [Guid("CFF5CD18-95FE-48C7-9418-4DE2CE8E69C3")]
        public class CUIAutomation { }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // UI Automation constants
        public const int UIA_ButtonControlTypeId = 50000;
        public const int UIA_NamePropertyId = 30005;
        public const int UIA_AutomationIdPropertyId = 30011;
        public const int UIA_IsEnabledPropertyId = 30010;
        public const int UIA_BoundingRectanglePropertyId = 30001;
        public const int UIA_ClassNamePropertyId = 30012;
        public const int TreeScope_Children = 2;
        public const int TreeScope_Descendants = 4;

        private IUIAutomation automation;

        public UIAutomationHelper()
        {
            try
            {
                automation = new CUIAutomation() as IUIAutomation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize UI Automation: {ex.Message}");
                automation = null;
            }
        }

        public List<ButtonInfo> FindButtonsUsingUIAutomation(IntPtr windowHandle)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            
            if (automation == null)
            {
                Console.WriteLine("UI Automation not available");
                return buttons;
            }

            try
            {
                Console.WriteLine("Using UI Automation to find buttons...");
                
                // Get the automation element for the window
                var element = automation.ElementFromHandle(windowHandle);
                if (element == IntPtr.Zero)
                {
                    Console.WriteLine("Could not get UI Automation element for window");
                    return buttons;
                }

                // Create condition to find buttons
                var buttonCondition = automation.CreatePropertyCondition(UIA_ButtonControlTypeId, UIA_ButtonControlTypeId);
                if (buttonCondition == IntPtr.Zero)
                {
                    Console.WriteLine("Could not create button condition");
                    return buttons;
                }

                // Find all button elements
                var elementWrapper = Marshal.GetObjectForIUnknown(element) as IUIAutomationElement;
                if (elementWrapper != null)
                {
                    var foundElements = elementWrapper.FindAll(TreeScope_Descendants, buttonCondition);
                    if (foundElements != IntPtr.Zero)
                    {
                        // Process found elements
                        ProcessUIAutomationElements(foundElements, buttons);
                    }
                    else
                    {
                        Console.WriteLine("No button elements found via UI Automation");
                    }
                }

                Marshal.ReleaseComObject(elementWrapper);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI Automation search failed: {ex.Message}");
            }

            return buttons;
        }

        private void ProcessUIAutomationElements(IntPtr elementsPtr, List<ButtonInfo> buttons)
        {
            try
            {
                // This is a simplified approach - in a real implementation you'd need to 
                // properly handle the IUIAutomationElementArray interface
                Console.WriteLine("Processing UI Automation elements...");
                
                // For now, we'll indicate that UI Automation elements were found
                // but we'd need more complex marshalling to fully extract the data
                Console.WriteLine("UI Automation found button elements, but full extraction requires more complex implementation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing UI Automation elements: {ex.Message}");
            }
        }

        public bool IsUIAutomationAvailable()
        {
            return automation != null;
        }
    }
}