﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Binarysharp.MemoryManagement.Common.Helpers;
using Binarysharp.MemoryManagement.Marshaling;
using Binarysharp.MemoryManagement.Native;
using Binarysharp.MemoryManagement.Native.Enums;
using Binarysharp.MemoryManagement.Native.Structs;

namespace Binarysharp.MemoryManagement.Memory
{
    /// <summary>
    ///     Static core class providing tools for memory editing.
    /// </summary>
    public static class MemoryCore
    {
        #region Public Methods
        /// <summary>
        ///     Reserves a region of memory within the virtual address space of a specified process.
        /// </summary>
        /// <param name="processHandle">The handle to a process.</param>
        /// <param name="size">The size of the region of memory to allocate, in bytes.</param>
        /// <param name="protectionFlags">The memory protection for the region of pages to be allocated.</param>
        /// <param name="allocationFlags">The type of memory allocation.</param>
        /// <returns>The base address of the allocated region.</returns>
        public static IntPtr Allocate(SafeMemoryHandle processHandle, int size,
            MemoryProtectionFlags protectionFlags = MemoryProtectionFlags.ExecuteReadWrite,
            MemoryAllocationFlags allocationFlags = MemoryAllocationFlags.Commit)
        {
            // Check if the handle is valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");

            // Allocate a memory page
            var ret = NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, size, allocationFlags, protectionFlags);

            // Check whether the memory page is valid
            if (ret != IntPtr.Zero)
                return ret;

            // If the pointer isn't valid, throws an exception
            throw new Win32Exception($"Couldn't allocate memory of {size} byte(s).");
        }

        /// <summary>
        ///     Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        public static void CloseHandle(IntPtr handle)
        {
            // Check if the handle is valid
            HandleManipulationHelper.ValidateAsArgument(handle, "handle");

            // Close the handle
            if (!NativeMethods.CloseHandle(handle))
            {
                throw new Win32Exception($"Couldn't close he handle 0x{handle}.");
            }
        }

        /// <summary>
        ///     Releases a region of memory within the virtual address space of a specified process.
        /// </summary>
        /// <param name="processHandle">A handle to a process.</param>
        /// <param name="address">A pointer to the starting address of the region of memory to be freed.</param>
        public static void Free(SafeMemoryHandle processHandle, IntPtr address)
        {
            // Check if the handles are valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");
            HandleManipulationHelper.ValidateAsArgument(address, "address");

            // Free the memory
            if (!NativeMethods.VirtualFreeEx(processHandle, address, 0, MemoryReleaseFlags.Release))
            {
                // If the memory wasn't correctly freed, throws an exception
                throw new Win32Exception($"The memory page 0x{address.ToString("X")} cannot be freed.");
            }
        }

        /// <summary>
        ///     etrieves information about the specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process to query.</param>
        /// <returns>A <see cref="ProcessBasicInformation" /> structure containg process information.</returns>
        public static ProcessBasicInformation NtQueryInformationProcess(SafeMemoryHandle processHandle)
        {
            // Check if the handle is valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");

            // Create a structure to store process info
            var info = new ProcessBasicInformation();

            // Get the process info
            var ret = NativeMethods.NtQueryInformationProcess(processHandle,
                ProcessInformationClass.ProcessBasicInformation, ref info, info.Size, IntPtr.Zero);

            // If the function succeeded
            if (ret == 0)
                return info;

            // Else, couldn't get the process info, throws an exception
            throw new ApplicationException($"Couldn't get the information from the process, error code '{ret}'.");
        }

        /// <summary>
        ///     Opens an existing local process object.
        /// </summary>
        /// <param name="accessFlags">The access level to the process object.</param>
        /// <param name="processId">The identifier of the local process to be opened.</param>
        /// <returns>An open handle to the specified process.</returns>
        public static SafeMemoryHandle OpenProcess(ProcessAccessFlags accessFlags, int processId)
        {
            // Get an handle from the remote process
            var handle = NativeMethods.OpenProcess(accessFlags, false, processId);

            // Check whether the handle is valid
            if (!handle.IsInvalid && !handle.IsClosed)
                return handle;

            // Else the handle isn't valid, throws an exception
            throw new Win32Exception($"Couldn't open the process {processId}.");
        }

        /// <summary>
        ///     Reads an array of bytes in the memory form the target process.
        /// </summary>
        /// <param name="processHandle">A handle to the process with memory that is being read.</param>
        /// <param name="address">A pointer to the base address in the specified process from which to read.</param>
        /// <param name="size">The number of bytes to be read from the specified process.</param>
        /// <returns>The collection of read bytes.</returns>
        public static byte[] ReadBytes(SafeMemoryHandle processHandle, IntPtr address, int size)
        {
            // Check if the handles are valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");
            HandleManipulationHelper.ValidateAsArgument(address, "address");

            // Allocate the buffer
            var buffer = new byte[size];
            int nbBytesRead;

            // Read the data from the target process
            if (NativeMethods.ReadProcessMemory(processHandle, address, buffer, size, out nbBytesRead) &&
                size == nbBytesRead)
                return buffer;

            // Else the data couldn't be read, throws an exception
            throw new Win32Exception($"Couldn't read {size} byte(s) from 0x{address.ToString("X")}.");
        }

        /// <summary>
        ///     Changes the protection on a region of committed pages in the virtual address space of a specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process whose memory protection is to be changed.</param>
        /// <param name="address">
        ///     A pointer to the base address of the region of pages whose access protection attributes are to be
        ///     changed.
        /// </param>
        /// <param name="size">The size of the region whose access protection attributes are changed, in bytes.</param>
        /// <param name="protection">The memory protection option.</param>
        /// <returns>The old protection of the region in a <see cref="MemoryBasicInformation" /> structure.</returns>
        public static MemoryProtectionFlags ChangeProtection(SafeMemoryHandle processHandle, IntPtr address, int size,
            MemoryProtectionFlags protection)
        {
            // Check if the handles are valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");
            HandleManipulationHelper.ValidateAsArgument(address, "address");

            // Create the variable storing the old protection of the memory page
            MemoryProtectionFlags oldProtection;

            // Change the protection in the target process
            if (NativeMethods.VirtualProtectEx(processHandle, address, size, protection, out oldProtection))
            {
                // Return the old protection
                return oldProtection;
            }

            // Else the protection couldn't be changed, throws an exception
            throw new Win32Exception(
                $"Couldn't change the protection of the memory at 0x{address.ToString("X")} of {size} byte(s) to {protection}.");
        }

        /// <summary>
        ///     Reads the value of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is read.</param>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        public static T Read<T>(SafeMemoryHandle processHandle, IntPtr address)
        {
            return MarshalType<T>.ByteArrayToObject(ReadBytes(processHandle, address, MarshalType<T>.Size));
        }

        /// <summary>
        ///     Reads the value of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is read.</param>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        public static T Read<T>(SafeMemoryHandle processHandle, Enum address)
        {
            return Read<T>(processHandle, new IntPtr(Convert.ToInt64(address)));
        }

        /// <summary>
        ///     Reads an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is read.</param>
        /// <param name="count">The number of cells in the array.</param>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        /// <returns>An array.</returns>
        public static T[] Read<T>(SafeMemoryHandle processHandle, IntPtr address, int count)
        {
            // Allocate an array to store the results
            var array = new T[count];
            // Read the array in the remote process
            for (var i = 0; i < count; i++)
            {
                array[i] = Read<T>(processHandle, address + MarshalType<T>.Size*i);
            }
            return array;
        }

        /// <summary>
        ///     Reads an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is read.</param>
        /// <param name="count">The number of cells in the array.</param>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        /// <returns>An array.</returns>
        public static T[] Read<T>(SafeMemoryHandle processHandle, Enum address, int count)
        {
            return Read<T>(processHandle, new IntPtr(Convert.ToInt64(address)), count);
        }

        /// <summary>
        ///     Retrieves information about a range of pages within the virtual address space of a specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        /// <param name="baseAddress">A pointer to the base address of the region of pages to be queried.</param>
        /// <returns>
        ///     A <see cref="MemoryBasicInformation" /> structures in which information about the specified page range is
        ///     returned.
        /// </returns>
        public static MemoryBasicInformation Query(SafeMemoryHandle processHandle, IntPtr baseAddress)
        {
            // Allocate the structure to store information of memory
            MemoryBasicInformation memoryInfo;

            // Query the memory region
            if (
                NativeMethods.VirtualQueryEx(processHandle, baseAddress, out memoryInfo,
                    MarshalType<MemoryBasicInformation>.Size) != 0)
                return memoryInfo;

            // Else the information couldn't be got
            throw new Win32Exception($"Couldn't query information about the memory region 0x{baseAddress.ToString("X")}");
        }

        /// <summary>
        ///     Retrieves information about a range of pages within the virtual address space of a specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process whose memory information is queried.</param>
        /// <param name="addressFrom">A pointer to the starting address of the region of pages to be queried.</param>
        /// <param name="addressTo">A pointer to the ending address of the region of pages to be queried.</param>
        /// <returns>A collection of <see cref="MemoryBasicInformation" /> structures.</returns>
        public static IEnumerable<MemoryBasicInformation> Query(SafeMemoryHandle processHandle, IntPtr addressFrom,
            IntPtr addressTo)
        {
            // Check if the handle is valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");

            // Convert the addresses to Int64
            var numberFrom = addressFrom.ToInt64();
            var numberTo = addressTo.ToInt64();

            // The first address must be lower than the second
            if (numberFrom >= numberTo)
                throw new ArgumentException("The starting address must be lower than the ending address.", "addressFrom");

            // Create the variable storing the result of the call of VirtualQueryEx
            int ret;

            // Enumerate the memory pages
            do
            {
                // Allocate the structure to store information of memory
                MemoryBasicInformation memoryInfo;

                // Get the next memory page
                ret = NativeMethods.VirtualQueryEx(processHandle, new IntPtr(numberFrom), out memoryInfo,
                    MarshalType<MemoryBasicInformation>.Size);

                // Increment the starting address with the size of the page
                numberFrom += memoryInfo.RegionSize;

                // Return the memory page
                if (memoryInfo.State != MemoryStateFlags.Free)
                    yield return memoryInfo;
            } while (numberFrom < numberTo && ret != 0);
        }

        /// <summary>
        ///     Writes data to an area of memory in a specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">A pointer to the base address in the specified process to which data is written.</param>
        /// <param name="byteArray">A buffer that contains data to be written in the address space of the specified process.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteBytes(SafeMemoryHandle processHandle, IntPtr address, byte[] byteArray)
        {
            // Check if the handles are valid
            HandleManipulationHelper.ValidateAsArgument(processHandle, "processHandle");
            HandleManipulationHelper.ValidateAsArgument(address, "address");

            // Create the variable storing the number of bytes written
            int nbBytesWritten;

            // Write the data to the target process
            if (NativeMethods.WriteProcessMemory(processHandle, address, byteArray, byteArray.Length, out nbBytesWritten))
            {
                // Check whether the length of the data written is equal to the inital array
                if (nbBytesWritten == byteArray.Length)
                    return nbBytesWritten;
            }

            // Else the data couldn't be written, throws an exception
            throw new Win32Exception($"Couldn't write {byteArray.Length} bytes to 0x{address.ToString("X")}");
        }

        /// <summary>
        ///     Reads a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public static string ReadString(SafeMemoryHandle processHandle, IntPtr address, Encoding encoding,
            bool isRelative = true, int maxLength = 512)
        {
            // Read the string
            var data = encoding.GetString(ReadBytes(processHandle, address, maxLength));
            // Search the end of the string
            var end = data.IndexOf('\0');
            // Crop the string with this end
            return data.Substring(0, end);
        }

        /// <summary>
        ///     Reads a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public static string ReadString(SafeMemoryHandle processHandle, Enum address, Encoding encoding,
            bool isRelative = true, int maxLength = 512)
        {
            return ReadString(processHandle, new IntPtr(Convert.ToInt64(address)), encoding, isRelative, maxLength);
        }

        /// <summary>
        ///     Reads a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public static string ReadString(SafeMemoryHandle processHandle, IntPtr address, bool isRelative = true,
            int maxLength = 512)
        {
            return ReadString(processHandle, address, Encoding.UTF8, isRelative, maxLength);
        }

        /// <summary>
        ///     Reads a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public static string ReadString(SafeMemoryHandle processHandle, Enum address, bool isRelative = true,
            int maxLength = 512)
        {
            return ReadString(processHandle, new IntPtr(Convert.ToInt64(address)), isRelative, maxLength);
        }

        /// <summary>
        ///     Writes the values of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the value is written.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void Write<T>(SafeMemoryHandle processHandle, IntPtr address, T value, bool isRelative = true)
        {
            WriteBytes(processHandle, address, MarshalType<T>.ObjectToByteArray(value));
        }

        /// <summary>
        ///     Writes the values of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the value is written.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void Write<T>(SafeMemoryHandle processHandle, Enum address, T value, bool isRelative = true)
        {
            Write(processHandle, new IntPtr(Convert.ToInt64(address)), value, isRelative);
        }

        /// <summary>
        ///     Writes an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the values is written.</param>
        /// <param name="array">The array to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void Write<T>(SafeMemoryHandle processHandle, IntPtr address, T[] array, bool isRelative = true)
        {
            // Write the array in the remote process
            for (var i = 0; i < array.Length; i++)
            {
                Write(processHandle, address + MarshalType<T>.Size*i, array[i], isRelative);
            }
        }

        /// <summary>
        ///     Writes an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the values is written.</param>
        /// <param name="array">The array to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void Write<T>(SafeMemoryHandle processHandle, Enum address, T[] array, bool isRelative = true)
        {
            Write(processHandle, new IntPtr(Convert.ToInt64(address)), array, isRelative);
        }

        /// <summary>
        ///     Writes a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void WriteString(SafeMemoryHandle processHandle, IntPtr address, string text, Encoding encoding,
            bool isRelative = true)
        {
            // Write the text
            WriteBytes(processHandle, address, encoding.GetBytes(text + '\0'));
        }

        /// <summary>
        ///     Writes a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void WriteString(SafeMemoryHandle processHandle, Enum address, string text, Encoding encoding,
            bool isRelative = true)
        {
            WriteString(processHandle, new IntPtr(Convert.ToInt64(address)), text, encoding, isRelative);
        }

        /// <summary>
        ///     Writes a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void WriteString(SafeMemoryHandle processHandle, IntPtr address, string text,
            bool isRelative = true)
        {
            WriteString(processHandle, address, text, Encoding.UTF8, isRelative);
        }

        /// <summary>
        ///     Writes a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="processHandle">A handle to the process memory to be modified.</param>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public static void WriteString(SafeMemoryHandle processHandle, Enum address, string text, bool isRelative = true)
        {
            WriteString(processHandle, new IntPtr(Convert.ToInt64(address)), text, isRelative);
        }
        #endregion
    }
}