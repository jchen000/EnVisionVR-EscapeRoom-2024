using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnVisionManager : MonoBehaviour
{
    public GameObject dialogPrefab;

    private StudyUIManager studyUI_;
    private Logger logger_;    
    private int participantId_ = -1;
    private int anchorId_ = -1;
    private string condition_ = "UNSET";
    private StudyTaskState taskState_ = StudyTaskState.Condition_Start;
    private bool taskStateUpdated_ = false;

    enum StudyTaskState
    {
        Condition_Start,
        Practice,
        Practice_Done,
        Task1_Pre,
        Task1_InProgress,
        Task1_Done,
        Task2_Pre,
        Task2_InProgress,
        Task2_Done,
        Task3_Pre,
        Task3_InProgress,
        Task3_Done,
        Condition_Complete
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        studyUI_ = transform.GetComponentInChildren<StudyUIManager>();

        string logFilename = "envision_" + System.DateTime.Now.ToString("yyyy_MMM_dd__hh_mm_ss") + ".log";
        logger_ = new Logger(logFilename);
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ShowOnStartDialog());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            taskStateUpdated_ = true;
        }

        if (taskStateUpdated_)
        {
            taskStateUpdated_ = false;

            AdvanceTaskState();
        }
    }

    private void ShowDialog(string text, bool editable, Action<string> onSubmitAction)
    {
        GameObject dialog = GameObject.Instantiate(dialogPrefab, Vector3.zero, Quaternion.identity, transform);
        Transform cameraT = Camera.main.transform;
        Vector3 cameraPos = cameraT.position;
        float cameraYaw = cameraT.rotation.eulerAngles.y;
        dialog.transform.position = cameraPos;
        dialog.transform.Rotate(Vector3.up, cameraYaw);
        dialog.transform.Translate(0, 0, 0.5f);

        DialogManager dialogManager = dialog.GetComponent<DialogManager>();
        dialogManager.SetDialogText(text);
        dialogManager.SetEditable(editable);
        dialogManager.SetInputText("");
        dialogManager.submitDialog = onSubmitAction;
    }


    private void SetParticipantId(string text)
    {
        int id = int.Parse(text);
        participantId_ = id;
        Debug.Log("Participante ID: " + id);
                
        LogEvent("participant_id," + id, true);

        StartCoroutine(ShowAnchorDialog());
    }

    private void SetAnchoId(string text)
    {
        int id = int.Parse(text);
        anchorId_ = id;
        Debug.Log("Anchor ID: " + id);

        LogEvent("anchor_id," + id, true);


        string[,] conditionArray = new string[12, 2] {  { "NVR", "EVR" },
                                                        { "EVR", "NVR" },
                                                        { "EVR", "NVR" },
                                                        { "NVR", "EVR" },
                                                        { "NVR", "EVR" },
                                                        { "EVR", "NVR" },
                                                        { "EVR", "NVR" },
                                                        { "NVR", "EVR" },
                                                        { "NVR", "EVR" },
                                                        { "EVR", "NVR" },
                                                        { "EVR", "NVR" },
                                                        { "NVR", "EVR" } };

        condition_ = conditionArray[participantId_ - 1, anchorId_ - 1];

        LogEvent("condition," + condition_, true);

        StartCoroutine(LoadPracticeSceneAsync());
    }

    private IEnumerator ShowOnStartDialog()
    {
        yield return new WaitForSeconds(2.0f);

        studyUI_.SetDisplayText("Enter Participant ID (Press <enter> to confirm)");
        ShowDialog("Participant ID:", true, SetParticipantId);
    }

    private IEnumerator ShowAnchorDialog()
    {
        yield return new WaitForSeconds(1.0f);

        studyUI_.SetDisplayText("Enter Anchor ID (Press <enter> to confirm)");
        ShowDialog("Anchor ID:", true, SetAnchoId);
    }

    private void SetAnchorPosition(int anchorId)
    {
        Transform cameraOffet = GameObject.Find("XR Rig/Camera Offset").transform;
        float yOffset = 0.3f; 
        
        if (anchorId == 1)
        {
            //anchorT = GameObject.Find("TeleportAnchors/Teleportation Anchor (7)").transform;
            cameraOffet.position = new Vector3(-6.689f, yOffset, 0.341f);
        }
        else if (anchorId == 2)
        {
            //anchorT = GameObject.Find("TeleportAnchors/Teleportation Anchor (4)").transform;
            cameraOffet.position = new Vector3(1.83f, yOffset, 0.918f);            
        }
    }

    private void ConfigurePracticeScene()
    {
        taskState_ = StudyTaskState.Practice;
        //SetAnchorPosition(anchorId_);

        // If no-assistance condition, deactivate EnVisionVR
        if (condition_ == "NVR")
        {
            GameObject.Find("EnVisionVR").SetActive(false);
        }
        else
        {
            GameObject.Find("EnVisionVR").GetComponent<SpeechRecognition>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<SceneIntroduction>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<CameraFieldOfView>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<ObjectLocalization>().OnLogEventAction = LogEvent;
        }

        studyUI_.SetDisplayText(string.Format("Practice Scene Loaded - Condition: {0} (Press <space> to move to test scene)", condition_));
    }

    private void ConfigureScene()
    {
        SetAnchorPosition(anchorId_);

        // If no-assistance condition, deactivate EnVisionVR
        if (condition_ == "NVR")
        {
            GameObject.Find("EnVisionVR").SetActive(false);
        }
        else
        {
            GameObject.Find("EnVisionVR").GetComponent<SpeechRecognition>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<SceneIntroduction>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<CameraFieldOfView>().OnLogEventAction = LogEvent;
            GameObject.Find("EnVisionVR").GetComponent<ObjectLocalization>().OnLogEventAction = LogEvent;
        }

        //studyUI_.SetDisplayText(string.Format("Escape Room Loaded - Condition: {0} (Press <space> to move to Task 1)", condition_));
    }

    private IEnumerator LoadPracticeSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("PracticeScene");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        LogEvent("scene_loaded", true);
        ConfigurePracticeScene();

        yield return null;
    }

    private IEnumerator LoadEscapeRoomSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("EscapeRoom");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {            
            yield return null;
        }
                        
        LogEvent("scene_loaded", true);
        ConfigureScene();

        yield return null;
    }

    private void LogEvent(string logtext)
    {
        LogEvent(logtext, true);
    }

    private void LogEvent(string logtext, bool timestamp)
    {
        if (timestamp)
        {
            logtext += string.Format(",{0}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        Debug.Log(logtext);
        logger_.WriteLine(logtext);
    }

    private void AdvanceTaskState()
    {
        taskState_++;

        string displayText = "";

        switch (taskState_)
        {
            case StudyTaskState.Practice_Done:
                displayText = string.Format("Escape Room Loaded - Condition: {0} (Press <space> to move to Task 1)", condition_);
                StartCoroutine(LoadEscapeRoomSceneAsync());
                break;

            case StudyTaskState.Task1_Pre:
                displayText = "Task 1 (Press <space> to indicate Task 1 has started)";
                break;

            case StudyTaskState.Task1_InProgress:
                displayText = "Task 1 in progress (Press <space> to mark Task 1 as done)";
                break;

            case StudyTaskState.Task1_Done:
                displayText = "Task 1 is done (Press <space> to move to Task 2)";
                break;

            case StudyTaskState.Task2_Pre:
                displayText = "Task 2 (Press <space> to indicate Task 2 has started)";
                break;

            case StudyTaskState.Task2_InProgress:
                displayText = "Task 2 in progress (Press <space> to mark Task 2 as done)";
                break;

            case StudyTaskState.Task2_Done:
                displayText = "Task 2 is done (Press <space> to move to Task 3)";
                break;

            case StudyTaskState.Task3_Pre:
                displayText = "Task 3 (Press <space> to indicate Task 3 has started)";
                break;

            case StudyTaskState.Task3_InProgress:
                displayText = "Task 3 in progress (Press <space> to mark Task 3 as done)";
                break;

            case StudyTaskState.Task3_Done:
                displayText = "Task 3 is done (Press <esc> to quit)";
                break;
        }

        studyUI_.SetDisplayText(displayText);
        LogEvent("task_state_change," + displayText, true);
    }
}
