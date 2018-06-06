/*========================================================================================
    GazepointClient                                                                  *//**
	
    A client for interfacing with an active Gazepoint 2.0 server instance running on the 
    same machine. 
	
    Copyright 2018 Erick Fernandez de Arteaga. All rights reserved.
        https://www.linkedin.com/in/erick-fda
        https://github.com/erick-fda
        https://bitbucket.org/erick-fda
	
*//*====================================================================================*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace GazepointUnityPackage
{
public class GazepointClient : MonoBehaviour
{
	/*----------------------------------------------------------------------------------------
		Instance Fields
	----------------------------------------------------------------------------------------*/
	/* Client data */
    [SerializeField] private int _serverPort = 4242;
    [SerializeField] private string _serverAddress = "127.0.0.1";
    [SerializeField] private int _bufferSize = 1024;   /* Increase this if reading incomplete data from server. */
    private TcpClient _client;
    private NetworkStream _dataStream;
    private StreamWriter _dataWriter;
    private string _dataIn;
    
    /* Data record types */
    public GazepointRecordType RECORD_DATA = new GazepointRecordType("DATA");
    public GazepointRecordType RECORD_CURSOR = new GazepointRecordType("CURSOR");
    public GazepointRecordType RECORD_POG_FIX = new GazepointRecordType("POG_FIX");
    public GazepointRecordType RECORD_TIME = new GazepointRecordType("TIME");
    private List<GazepointRecordType> _recordTypes;

    /* Data reading options */
    [SerializeField] private bool _enableReadCursor = false;
    [SerializeField] private bool _enableReadPogFix = false;
    [SerializeField] private bool _enableReadTime = false;

    /* String constants */
    private const string RECORD_REGEX = "<REC[^>]*>";
    private const string ENABLE_SEND = "<SET ID=\"ENABLE_SEND_{0}\" STATE=\"{1}\" />\r\n";

	/*----------------------------------------------------------------------------------------
		Instance Properties
	----------------------------------------------------------------------------------------*/
	
    
	/*----------------------------------------------------------------------------------------
		MonoBehaviour Methods
	----------------------------------------------------------------------------------------*/
    /**
        Initialize client connection and record reading.
    */
    private void Start()
    {
        /* Try creating client and report error if any. */
        try
        {
            _client = new TcpClient(_serverAddress, _serverPort);
        }
        catch (Exception e)
        {
            Debug.LogError("GazepointClient.Start(): Failed to create client with error: " + e);
        }

        /* Set up writer. */
        _dataStream = _client.GetStream();
        _dataWriter = new StreamWriter(_dataStream);

        InitRecordReading();

        _dataWriter.Flush();
    }

    /**
        Read enabled records.
    */
    private void Update()
    {
        /* Read the latest data from the stream into a buffer. */
        byte[] buffer = new byte[_bufferSize];

        while (_dataStream.DataAvailable)
        {
            _dataStream.Read(buffer, 0, buffer.Length);
        }

        _dataIn += Encoding.UTF8.GetString(buffer);

        /* Extract the first available record. */
        Match m = Regex.Match(_dataIn, RECORD_REGEX);
        if (m.Success)
        {
            _dataIn = m.Value;

            /* Read the record. */
            double time_val;
            double fpogx;
            double fpogy;
            int fpog_valid;
            int startindex, endindex;

            // Process _dataIn string to extract FPOGX, FPOGY, etc...
            startindex = _dataIn.IndexOf("TIME=\"") + "TIME=\"".Length;
            endindex = _dataIn.IndexOf("\"", startindex);
            time_val = Double.Parse(_dataIn.Substring(startindex, endindex - startindex));

            startindex = _dataIn.IndexOf("FPOGX=\"") + "FPOGX=\"".Length;
            endindex = _dataIn.IndexOf("\"", startindex);
            fpogx = Double.Parse(_dataIn.Substring(startindex, endindex - startindex));

            startindex = _dataIn.IndexOf("FPOGY=\"") + "FPOGY=\"".Length;
            endindex = _dataIn.IndexOf("\"", startindex);
            fpogy = Double.Parse(_dataIn.Substring(startindex, endindex - startindex));

            startindex = _dataIn.IndexOf("FPOGV=\"") + "FPOGV=\"".Length;
            endindex = _dataIn.IndexOf("\"", startindex);
            fpog_valid = Int32.Parse(_dataIn.Substring(startindex, endindex - startindex));

            Debug.Log(string.Format("Raw data: {0}", _dataIn));
            Debug.Log(string.Format("Processed data: Time {0}, Gaze ({1},{2}) Valid={3}", time_val, fpogx, fpogy, fpog_valid));
        }

        _dataIn = "";
    }

    /**
        Close client connection.
    */
    private void OnDestroy()
    {
        _dataWriter.Close();
        _dataStream.Close();
        _client.Close();
    }
    
	/*----------------------------------------------------------------------------------------
		Instance Methods
	----------------------------------------------------------------------------------------*/
    /**
        Initialize the reading of enabled records.
    */
    private void InitRecordReading()
    {
        SetRead(RECORD_CURSOR, _enableReadCursor);
        SetRead(RECORD_POG_FIX, _enableReadPogFix);
        SetRead(RECORD_TIME, _enableReadTime);
        SetRead(RECORD_DATA, true);
    }

    /**
        Populate the record types list with all record types.
    */
    private void InitRecordTypesList()
    {
        _recordTypes.Add(RECORD_DATA);
        _recordTypes.Add(RECORD_CURSOR);
        _recordTypes.Add(RECORD_POG_FIX);
        _recordTypes.Add(RECORD_TIME);
    }

    /**
        Set whether to read records of the given type.
    */
	public void SetRead(GazepointRecordType record, bool read)
    {
        _dataWriter.Write(string.Format(ENABLE_SEND, record.EnableId, read ? 1 : 0));
    }

    /**
        Set whether to read all records.

        Reading records affects performance. Do not enable reading all records unless you 
        are actually using the data. This function is primarily provided for the purpose 
        of allowing users to easily DISABLE reading all records.
    */
    public void SetReadAll(bool read)
    {
        foreach (GazepointRecordType eachRecord in _recordTypes)
        {
            _dataWriter.Write(string.Format(ENABLE_SEND, eachRecord.EnableId, read ? 1 : 0));
        }
    }

    /*----------------------------------------------------------------------------------------
		Private Classes
	----------------------------------------------------------------------------------------*/
	/**
        Defines a data record type.
    */
    public class GazepointRecordType
    {
        public readonly string EnableId;

        public GazepointRecordType (string enableId)
        {
            EnableId = enableId;
        }
    }
}}
