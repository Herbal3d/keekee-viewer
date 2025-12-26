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
    public delegate OMVSD.OSD ProcessGetCallback(IRestHandler handler, Uri uri, string after);
    public delegate OMVSD.OSD ProcessPostCallback(IRestHandler handler, Uri uri, string after, OMVSD.OSD body);

    public interface IRestHandler {


        void GetPostAsync(string afterString);

        OMVSD.OSD ProcessGetParam(IRestHandler handler, Uri uri, string afterString);
        OMVSD.OSD ProcessPostParam(IRestHandler handler, Uri uri, string afterString, OMVSD.OSD rawbody);

        const string APINAME = "/api";

        const string RESTREQUESTERRORCODE = "RESTRequestError";
        const string RESTREQUESTERRORMSG = "RESTRequestMsg";

        HttpListener Handler { get; }
        string BaseUrl { get; }
        ProcessGetCallback? ProcessGet { get; }
        ProcessPostCallback? ProcessPost { get; }
        IDisplayable? Displayable { get; }
        string? Dir { get; }
        bool ParameterSetWritable { get; }
        string Prefix { get; }

        HttpListenerContext? ListenerContext { get; set; }
        HttpListenerRequest? ListenerRequest { get; set; }
        HttpListenerResponse? ListenerResponse { get; set; }

    }


}