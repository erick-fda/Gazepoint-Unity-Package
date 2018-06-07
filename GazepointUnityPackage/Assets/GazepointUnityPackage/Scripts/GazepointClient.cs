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
    [SerializeField] private int _bufferSize = 1024;   /* Increase this if _dataIn is coming up empty. */
    private TcpClient _client;
    private NetworkStream _dataStream;
    private StreamWriter _dataWriter;
    private string _dataIn = "";

    /* String constants */
    private const string RECORD_REGEX = "<REC[^>]*>";
    private const string ENABLE_SEND = "<SET ID=\"ENABLE_SEND_{0}\" STATE=\"{1}\" />\r\n";
    private const string FIELD_START_FLAG = "{0}=\"";
    private const string FIELD_END_FLAG = "\"";
    
    /* Record types */
    public GazepointRecordType RECORD_DATA = new GazepointRecordType("DATA");
    public GazepointRecordType RECORD_COUNTER = new GazepointRecordType("COUNTER");
    public GazepointRecordType RECORD_CURSOR = new GazepointRecordType("CURSOR");
    public GazepointRecordType RECORD_EYE_LEFT = new GazepointRecordType("EYE_LEFT");
    public GazepointRecordType RECORD_EYE_RIGHT = new GazepointRecordType("EYE_RIGHT");
    public GazepointRecordType RECORD_POG_FIX = new GazepointRecordType("POG_FIX");
    public GazepointRecordType RECORD_POG_LEFT = new GazepointRecordType("POG_LEFT");
    public GazepointRecordType RECORD_POG_RIGHT = new GazepointRecordType("POG_RIGHT");
    public GazepointRecordType RECORD_POG_BEST = new GazepointRecordType("POG_BEST");
    public GazepointRecordType RECORD_PUPIL_LEFT = new GazepointRecordType("PUPIL_LEFT");
    public GazepointRecordType RECORD_PUPIL_RIGHT = new GazepointRecordType("PUPIL_RIGHT");
    public GazepointRecordType RECORD_TIME = new GazepointRecordType("TIME");
    public GazepointRecordType RECORD_TIME_TICK = new GazepointRecordType("TIME_TICK");
    public GazepointRecordType RECORD_USER_DATA = new GazepointRecordType("USER_DATA");
    private List<GazepointRecordType> _recordTypes;

    /* Record reading options */
    [SerializeField] private bool _enableReadCounter = false;
    [SerializeField] private bool _enableReadCursor = false;
    [SerializeField] private bool _enableReadLeftEye = false;
    [SerializeField] private bool _enableReadRightEye = false;
    [SerializeField] private bool _enableReadFixedPog = false;
    [SerializeField] private bool _enableReadLeftPog = false;
    [SerializeField] private bool _enableReadRightPog = false;
    [SerializeField] private bool _enableReadBestPog = false;
    [SerializeField] private bool _enableReadLeftPupil = false;
    [SerializeField] private bool _enableReadRightPupil = false;
    [SerializeField] private bool _enableReadTime = false;
    [SerializeField] private bool _enableReadTimeTick = false;
    [SerializeField] private bool _enableReadUserData = false;

    /* Record field IDs */
    private const string COUNTER_ID = "CNT";
    private const string CURSOR_X_ID = "CX";
    private const string CURSOR_Y_ID = "CY";
    private const string CURSOR_STATE_ID = "CS";
    private const string LEFT_EYE_X_ID = "LEYEX";
    private const string LEFT_EYE_Y_ID = "LEYEY";
    private const string LEFT_EYE_Z_ID = "LEYEZ";
    private const string LEFT_EYE_PUPIL_DIAMETER_ID = "LPUPILD";
    private const string LEFT_EYE_PUPIL_VALID_ID = "LPUPILV";
    private const string RIGHT_EYE_X_ID = "REYEX";
    private const string RIGHT_EYE_Y_ID = "REYEY";
    private const string RIGHT_EYE_Z_ID = "REYEZ";
    private const string RIGHT_EYE_PUPIL_DIAMETER_ID = "RPUPILD";
    private const string RIGHT_EYE_PUPIL_VALID_ID = "RPUPILV";
    private const string FIXED_POG_X_ID = "FPOGX";
    private const string FIXED_POG_Y_ID = "FPOGY";
    private const string FIXED_POG_START_ID = "FPOGS";
    private const string FIXED_POG_DURATION_ID = "FPOGD";
    private const string FIXED_POG_ID_ID = "FPOGID";
    private const string FIXED_POG_VALID_ID = "FPOGV";
    private const string LEFT_POG_X_ID = "LPOGX";
    private const string LEFT_POG_Y_ID = "LPOGY";
    private const string LEFT_POG_VALID_ID = "LPOGV";
    private const string RIGHT_POG_X_ID = "RPOGX";
    private const string RIGHT_POG_Y_ID = "RPOGY";
    private const string RIGHT_POG_VALID_ID = "RPOGV";
    private const string BEST_POG_X_ID = "BPOGX";
    private const string BEST_POG_Y_ID = "BPOGY";
    private const string BEST_POG_VALID_ID = "BPOGV";
    private const string LEFT_PUPIL_X_ID = "LPCX";
    private const string LEFT_PUPIL_Y_ID = "LPCY";
    private const string LEFT_PUPIL_DIAMETER_ID = "LPD";
    private const string LEFT_PUPIL_SCALE_ID = "LPS";
    private const string LEFT_PUPIL_VALID_ID = "LPV";
    private const string RIGHT_PUPIL_X_ID = "RPCX";
    private const string RIGHT_PUPIL_Y_ID = "RPCY";
    private const string RIGHT_PUPIL_DIAMETER_ID = "RPD";
    private const string RIGHT_PUPIL_SCALE_ID = "RPS";
    private const string RIGHT_PUPIL_VALID_ID = "RPV";
    private const string TIME_ID = "TIME";
    private const string TIME_TICK_ID = "TIME_TICK";
    private const string USER_DATA_ID = "USER";

    /* Record field variables */
    private int _counter;
    private float _cursorX;
    private float _cursorY;
    private int _cursorState;
    private float _leftEyeX;
    private float _leftEyeY;
    private float _leftEyeZ;
    private float _leftEyePupilDiameter;
    private bool _leftEyePupilValid;
    private float _rightEyeX;
    private float _rightEyeY;
    private float _rightEyeZ;
    private float _rightEyePupilDiameter;
    private bool _rightEyePupilValid;
    private float _fixedPogX;
    private float _fixedPogY;
    private float _fixedPogStart;
    private float _fixedPogDuration;
    private int _fixedPogId;
    private bool _fixedPogValid;
    private float _leftPogX;
    private float _leftPogY;
    private bool _leftPogValid;
    private float _rightPogX;
    private float _rightPogY;
    private bool _rightPogValid;
    private float _bestPogX;
    private float _bestPogY;
    private bool _bestPogValid;
    private float _leftPupilX;
    private float _leftPupilY;
    private float _leftPupilDiameter;
    private float _leftPupilScale;
    private bool _leftPupilValid;
    private float _rightPupilX;
    private float _rightPupilY;
    private float _rightPupilDiameter;
    private float _rightPupilScale;
    private bool _rightPupilValid;
    private float _time;
    private ulong _timeTick;
    private string _userData = "";

	/*----------------------------------------------------------------------------------------
		Instance Properties
	----------------------------------------------------------------------------------------*/
	/* Record field properties */
    public int Counter { get { return _counter; } private set { _counter = value; } }
    public float CursorX { get { return _cursorX; } private set { _cursorX = value; } }
    public float CursorY { get { return _cursorY; } private set { _cursorY = value; } }
    public int CursorState { get { return _cursorState; } private set { _cursorState = value; } }
    public float LeftEyeX { get { return _leftEyeX; } private set { _leftEyeX = value; } }
    public float LeftEyeY { get { return _leftEyeY; } private set { _leftEyeY = value; } }
    public float LeftEyeZ { get { return _leftEyeZ; } private set { _leftEyeZ = value; } }
    public float LeftEyePupilDiameter { get { return _leftEyePupilDiameter; } private set { _leftEyePupilDiameter = value; } }
    public bool LeftEyePupilValid { get { return _leftEyePupilValid; } private set { _leftEyePupilValid = value; } }
    public float RightEyeX { get { return _rightEyeX; } private set { _rightEyeX = value; } }
    public float RightEyeY { get { return _rightEyeY; } private set { _rightEyeY = value; } }
    public float RightEyeZ { get { return _rightEyeZ; } private set { _rightEyeZ = value; } }
    public float RightEyePupilDiameter { get { return _rightEyePupilDiameter; } private set { _rightEyePupilDiameter = value; } }
    public bool RightEyePupilValid { get { return _rightEyePupilValid; } private set { _rightEyePupilValid = value; } }
    public float FixedPogX { get { return _fixedPogX; } private set { _fixedPogX = value; } }
    public float FixedPogY { get { return _fixedPogY; } private set { _fixedPogY = value; } }
    public float FixedPogStart { get { return _fixedPogStart; } private set { _fixedPogStart = value; } }
    public float FixedPogDuration { get { return _fixedPogDuration; } private set { _fixedPogDuration = value; } }
    public int FixedPogId { get { return _fixedPogId; } private set { _fixedPogId = value; } }
    public bool FixedPogValid { get { return _fixedPogValid; } private set { _fixedPogValid = value; } }
    public float LeftPogX { get { return _leftPogX; } private set { _leftPogX = value; } }
    public float LeftPogY { get { return _leftPogY; } private set { _leftPogY = value; } }
    public bool LeftPogValid { get { return _leftPogValid; } private set { _leftPogValid = value; } }
    public float RightPogX { get { return _rightPogX; } private set { _rightPogX = value; } }
    public float RightPogY { get { return _rightPogY; } private set { _rightPogY = value; } }
    public bool RightPogValid { get { return _rightPogValid; } private set { _rightPogValid = value; } }
    public float BestPogX { get { return _bestPogX; } private set { _bestPogX = value; } }
    public float BestPogY { get { return _bestPogY; } private set { _bestPogY = value; } }
    public bool BestPogValid { get { return _bestPogValid; } private set { _bestPogValid = value; } }
    public float LeftPupilX { get { return _leftPupilX; } private set { _leftPupilX = value; } }
    public float LeftPupilY { get { return _leftPupilY; } private set { _leftPupilY = value; } }
    public float LeftPupilDiameter { get { return _leftPupilDiameter; } private set { _leftPupilDiameter = value; } }
    public float LeftPupilScale { get { return _leftPupilScale; } private set { _leftPupilScale = value; } }
    public bool LeftPupilValid { get { return _leftPupilValid; } private set { _leftPupilValid = value; } }
    public float RightPupilX { get { return _rightPupilX; } private set { _rightPupilX = value; } }
    public float RightPupilY { get { return _rightPupilY; } private set { _rightPupilY = value; } }
    public float RightPupilDiameter { get { return _rightPupilDiameter; } private set { _rightPupilDiameter = value; } }
    public float RightPupilScale { get { return _rightPupilScale; } private set { _rightPupilScale = value; } }
    public bool RightPupilValid { get { return _rightPupilValid; } private set { _rightPupilValid = value; } }
    public float Time { get { return _time; } private set { _time = value; } }
    public ulong TimeTick { get { return _timeTick; } private set { _timeTick = value; } }
    public string UserData { get { return _userData; } private set { _userData = value; } }
    
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

        _dataIn = Encoding.UTF8.GetString(buffer);

        /* Extract the first available record. */
        Match m = Regex.Match(_dataIn, RECORD_REGEX);
        if (m.Success)
        {
            _dataIn = m.Value;
            UpdateValues();
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
        SetRead(RECORD_COUNTER, _enableReadCounter);
        SetRead(RECORD_CURSOR, _enableReadCursor);
        SetRead(RECORD_EYE_LEFT, _enableReadLeftEye);
        SetRead(RECORD_EYE_RIGHT, _enableReadRightEye);
        SetRead(RECORD_POG_FIX, _enableReadFixedPog);
        SetRead(RECORD_POG_LEFT, _enableReadLeftPog);
        SetRead(RECORD_POG_RIGHT, _enableReadRightPog);
        SetRead(RECORD_POG_BEST, _enableReadBestPog);
        SetRead(RECORD_PUPIL_LEFT, _enableReadLeftPupil);
        SetRead(RECORD_PUPIL_RIGHT, _enableReadRightPupil);
        SetRead(RECORD_TIME, _enableReadTime);
        SetRead(RECORD_TIME_TICK, _enableReadTimeTick);
        SetRead(RECORD_USER_DATA, _enableReadUserData);
        SetRead(RECORD_DATA, true);
    }

    /**
        Populate the record types list with all record types.
    */
    private void InitRecordTypesList()
    {
        _recordTypes.Add(RECORD_DATA);
        _recordTypes.Add(RECORD_COUNTER);
        _recordTypes.Add(RECORD_CURSOR);
        _recordTypes.Add(RECORD_EYE_LEFT);
        _recordTypes.Add(RECORD_EYE_RIGHT);
        _recordTypes.Add(RECORD_POG_FIX);
        _recordTypes.Add(RECORD_POG_LEFT);
        _recordTypes.Add(RECORD_POG_RIGHT);
        _recordTypes.Add(RECORD_POG_BEST);
        _recordTypes.Add(RECORD_PUPIL_LEFT);
        _recordTypes.Add(RECORD_PUPIL_RIGHT);
        _recordTypes.Add(RECORD_TIME);
        _recordTypes.Add(RECORD_TIME_TICK);
        _recordTypes.Add(RECORD_USER_DATA);
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

    /**
        Parses and returns the boolean value of the given field from the most recently read 
        record.
    */
    private bool ParseBool(string fieldId)
    {
        return ParseInt(fieldId) > 0;
    }

    /**
        Parses and returns the float value of the given field from the most recently read 
        record.
    */
    private float ParseFloat(string fieldId)
    {
        return (float) Double.Parse(ParseString(fieldId));
    }

    /**
        Parses and returns the int value of the given field from the most recently read 
        record.
    */
    private int ParseInt(string fieldId)
    {
        return Int32.Parse(ParseString(fieldId));
    }

    /**
        Parses and returns the string value of the given field from the most recently read 
        record.
    */
    private string ParseString(string fieldId)
    {
        int startIndex, endIndex;
        string startFlag = string.Format(FIELD_START_FLAG, fieldId);
        startIndex = _dataIn.IndexOf(startFlag) + startFlag.Length;
        endIndex = _dataIn.IndexOf(FIELD_END_FLAG, startIndex);
        return _dataIn.Substring(startIndex, endIndex - startIndex);
    }

    /**
        Parses and returns the ulong value of the given field from the most recently read 
        record.
    */
    private ulong ParseUlong(string fieldId)
    {
        return (ulong) Decimal.Parse(ParseString(fieldId));
    }

    /**
        Updates all data values being read from the server with the most recently sent 
        values.
    */
    private void UpdateValues()
    {
        if (_enableReadCounter)
        {
            Counter = ParseInt(COUNTER_ID);
        }
        
        if (_enableReadCursor)
        {
            CursorX = ParseFloat(CURSOR_X_ID);
            CursorY = ParseFloat(CURSOR_Y_ID);
            CursorState = ParseInt(CURSOR_STATE_ID);
        }
        
        if (_enableReadLeftEye)
        {
            LeftEyeX = ParseFloat(LEFT_EYE_X_ID);
            LeftEyeY = ParseFloat(LEFT_EYE_Y_ID);
            LeftEyeZ = ParseFloat(LEFT_EYE_Z_ID);
            LeftEyePupilDiameter = ParseFloat(LEFT_EYE_PUPIL_DIAMETER_ID);
            LeftEyePupilValid = ParseBool(LEFT_EYE_PUPIL_VALID_ID);
        }
        
        if (_enableReadRightEye)
        {
            RightEyeX = ParseFloat(RIGHT_EYE_X_ID);
            RightEyeY = ParseFloat(RIGHT_EYE_Y_ID);
            RightEyeZ = ParseFloat(RIGHT_EYE_Z_ID);
            RightEyePupilDiameter = ParseFloat(RIGHT_EYE_PUPIL_DIAMETER_ID);
            RightEyePupilValid = ParseBool(RIGHT_EYE_PUPIL_VALID_ID);
        }
        
        if (_enableReadFixedPog)
        {
            FixedPogX = ParseFloat(FIXED_POG_X_ID);
            FixedPogY = ParseFloat(FIXED_POG_Y_ID);
            FixedPogStart = ParseFloat(FIXED_POG_START_ID);
            FixedPogDuration = ParseFloat(FIXED_POG_DURATION_ID);
            FixedPogId = ParseInt(FIXED_POG_ID_ID);
            FixedPogValid = ParseBool(FIXED_POG_VALID_ID);
        }
        
        if (_enableReadLeftPog)
        {
            LeftPogX = ParseFloat(LEFT_POG_X_ID);
            LeftPogY = ParseFloat(LEFT_POG_Y_ID);
            LeftPogValid = ParseBool(LEFT_POG_VALID_ID);
        }
        
        if (_enableReadRightPog)
        {
            RightPogX = ParseFloat(RIGHT_POG_X_ID);
            RightPogY = ParseFloat(RIGHT_POG_Y_ID);
            RightPogValid = ParseBool(RIGHT_POG_VALID_ID);
        }
        
        if (_enableReadBestPog)
        {
            BestPogX = ParseFloat(BEST_POG_X_ID);
            BestPogY = ParseFloat(BEST_POG_Y_ID);
            BestPogValid = ParseBool(BEST_POG_VALID_ID);
        }
        
        if (_enableReadLeftPupil)
        {
            LeftPupilX = ParseFloat(LEFT_PUPIL_X_ID);
            LeftPupilY = ParseFloat(LEFT_PUPIL_Y_ID);
            LeftPupilDiameter = ParseFloat(LEFT_PUPIL_DIAMETER_ID);
            LeftPupilScale = ParseFloat(LEFT_PUPIL_SCALE_ID);
            LeftPupilValid = ParseBool(LEFT_PUPIL_VALID_ID);
        }
        
        if (_enableReadRightPupil)
        {
            RightPupilX = ParseFloat(RIGHT_PUPIL_X_ID);
            RightPupilY = ParseFloat(RIGHT_PUPIL_Y_ID);
            RightPupilDiameter = ParseFloat(RIGHT_PUPIL_DIAMETER_ID);
            RightPupilScale = ParseFloat(RIGHT_PUPIL_SCALE_ID);
            RightPupilValid = ParseBool(RIGHT_PUPIL_VALID_ID);
        }
        
        if (_enableReadTime)
        {
            Time = ParseFloat(TIME_ID);
        }
        
        if (_enableReadTimeTick)
        {
            TimeTick = ParseUlong(TIME_TICK_ID);
        }
        
        if (_enableReadUserData)
        {
            UserData = ParseString(USER_DATA_ID);
        }

        // Debug.Log(string.Format("Raw data: {0}", _dataIn));
        // Debug.Log(string.Format("Processed data: Time {0}, Gaze ({1},{2}) Valid={3}", Time, FixedPogX, FixedPogY, FixedPogValid));
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
