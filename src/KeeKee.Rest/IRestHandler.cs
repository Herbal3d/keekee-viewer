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

using System.Net;

using KeeKee.Framework;
using KeeKee.Rest;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {
    // called to process GET. The Uri is the full request uri and 'after' is everything after the 'api'
    public delegate OMVSD.OSD ProcessGetCallback(IRestHandler handler, Uri uri, string after,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse);
    public delegate OMVSD.OSD ProcessPostCallback(IRestHandler handler, Uri uri, string after, OMVSD.OSD body,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse);

    public interface IRestHandler {


        void ProcessGetOrPostRequest(HttpListenerContext context, HttpListenerRequest request, HttpListenerResponse response, string afterString);

        OMVSD.OSD ProcessGetParam(IRestHandler handler, Uri uri, string afterString,
            HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse);
        OMVSD.OSD ProcessPostParam(IRestHandler handler, Uri uri, string afterString, OMVSD.OSD rawbody,
            HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse);

        const string RESTREQUESTERRORCODE = "RESTRequestError";
        const string RESTREQUESTERRORMSG = "RESTRequestMsg";

        string BaseUrl { get; }
        // Optional callbacks to process GET and POST requests
        ProcessGetCallback? ProcessGet { get; }
        ProcessPostCallback? ProcessPost { get; }
        // Optional displayable interface to get parameters from
        IDisplayable? Displayable { get; }
        // Directory where static files are located for this handler
        string? Dir { get; }
        // The prefix string for this handler. The stuff after the 'api/' in the URL
        string Prefix { get; }
    }


}