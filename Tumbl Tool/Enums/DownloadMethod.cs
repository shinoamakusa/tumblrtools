﻿/* 01010011 01101000 01101001 01101110 01101111  01000001 01101101 01100001 01101011 01110101 01110011 01100001
 *
 *  Project: Tumblr Tools - Image parser and downloader from Tumblr blog system
 *
 *  Author: Shino Amakusa
 *
 *  Created: 2013
 *
 *  Last Updated: August, 2017
 *
 * 01010011 01101000 01101001 01101110 01101111  01000001 01101101 01100001 01101011 01110101 01110011 01100001 */

namespace Tumblr_Tool.Enums
{
    /// <summary>
    /// Image download internal methods
    /// </summary>
    public enum DownloadMethod
    {
        /// <summary>
        /// Use .Net WebClient Async file download method
        /// </summary>
        WebClientAsync,

        /// <summary>
        /// Use RestSharp file download method
        /// </summary>
        RestSharp,

        /// <summary>
        /// Use .Net WebClient sync file download method
        /// </summary>
        WebClient
    }
}