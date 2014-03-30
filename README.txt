WAMS VOD Butler


It is a sample code that takes all the plumbing on WAMS VOD and build an end to end process.

The workflow start when a user uploads media files in a storage area (what we are calling a staging area) the WAMS butler process (a worker role that is constantly running) 
start the process for each input media. It does the encode following the configuration defined in one table storage. It is mean, using the same code you could produce 
different king of output asset, for example MP4 multi bit rate, HLS, HDS, DAHS, Smooth etc. 

When the media asset is ready, the butler could send a notification to the customer (mail to human, message or services call  to CMS/customer portal). This soft integration 
gives the real possibility to implement end to end process including the CMS/portal content publication.


Configuration Process
0. Create a Storage Account in the same region/Afinity group that the Media Serrvices Storage 
Example name: wamsvodbutler

1. UPDATE the Services Configuration
1.1 Update strConnConfig key with the stoarga connection string to butler storage
Example
DefaultEndpointsProtocol=https;AccountName=wamsvodbuttler;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
1.2 Update appId KEY with you id implementation
Example: mywams01

2. Create storage table "wamsbutlerconfig"
2.1 Add entity 
<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>Active</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:06:00.4969139Z</d:Timestamp>
        <d:value>1</d:value>
</m:properties>
2.1 add entity
<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>AssetStorageConn</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:07:49.3809541Z</d:Timestamp>
        <d:value>[Storage Account connection string to WAMS storage]</d:value>
</m:properties>
2.3 Add entity
<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>ExternalStorageConn</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:08:01.8967024Z</d:Timestamp>
        <d:value>[Storage Account connection string to Stage Storage storage]</d:value>
</m:properties>
2.4 add entity
      <m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>ExternalStorageContainer</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:08:13.2065713Z</d:Timestamp>
        <d:value>stage</d:value>
      </m:properties>
2.5 add Entity
	<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>MediaAccountKey</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:09:17.4701443Z</d:Timestamp>
        <d:value>[WAMS KEY]</d:value>
      </m:properties>
2.6 Add Entity
	<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>MediaAccountName</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:09:27.7061206Z</d:Timestamp>
        <d:value>[WAMS services Name]</d:value>
      </m:properties>

3. Create a Table "wamsbutleroutputformat"
3.1 add entity (take care about OutTypesId type, it's must be Int32 )
<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>4</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-27T23:22:27.3518874Z</d:Timestamp>
        <d:EncodeDescription>Nascarv3.xml</d:EncodeDescription>
        <d:JobEncodeDescription>JOB multi bitrates Mp4</d:JobEncodeDescription>
        <d:MediaProcessorByName>Windows Azure Media Encoder</d:MediaProcessorByName>
        <d:NameTail>_MP4v2</d:NameTail>
        <d:OutTypesId m:type="Edm.Int32">3</d:OutTypesId>
        <d:TaskEncodeDescription>Task multi bitrates MP4</d:TaskEncodeDescription>
      </m:properties>
3.1 add entity
	<m:properties>
        <d:PartitionKey>mywams01</d:PartitionKey>
        <d:RowKey>5</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2014-03-19T14:11:06.9861556Z</d:Timestamp>
        <d:EncodeDescription>N/A</d:EncodeDescription>
        <d:JobEncodeDescription>N/A</d:JobEncodeDescription>
        <d:MediaProcessorByName>N/A</d:MediaProcessorByName>
        <d:NameTail>N/A</d:NameTail>
        <d:OutTypesId m:type="Edm.Int32">0</d:OutTypesId>
        <d:TaskEncodeDescription>N/A</d:TaskEncodeDescription>
      </m:properties>

4. Create a Queue wamsbutlerallencodefinish

5. Create a Blob storage Container "stage"

6. Create a Blob storage container "wamsvodbuterencodeconfig"


Running and test
0. Run in developer enviroment or deploy the solution.
1. Copy a MP4 video file into the stage blob storage
2. Now, you could see after 1 minute, the WAMS JOB running to encode your video in a multi bitrate MP4 and publish in WAMS
3. When the process end, you could read from the Queue on message with all the information of the process.

If you have any error, you could see in the table WADLogsTABLE en the Butler Storage.

Enjoy it.

Copyright © Microsoft Corporation.
All rights reserved.
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.  You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
