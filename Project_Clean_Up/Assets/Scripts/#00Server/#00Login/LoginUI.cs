using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Battlehub.Dispatcher;

public class LoginUI : MonoBehaviour
{
    private static LoginUI instance;    // 인스턴스

    // public GameObject mainTitle;
    // public GameObject subTitle;
    public GameObject touchStart;
    public GameObject loginObject;
    public GameObject customLoginObject;
    public GameObject signUpObject;
    public GameObject errorObject;
    public GameObject nicknameObject;

    private TMP_InputField[] loginField;
    private TMP_InputField[] signUpField;
    private TMP_InputField nicknameField;
    private Text errorText;
    private GameObject loadingObject;

    private const byte ID_INDEX = 0;
    private const byte PW_INDEX = 1;
    private const string VERSION_STR = "Ver {0}";

    void Awake()
    {
        instance = this;
    }

    public static LoginUI GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("LoginUI 인스턴스가 존재하지 않습니다.");
            return null;
        }
        return instance;
    }

    void Start()
    {
        // mainTitle.SetActive(true);
        touchStart.SetActive(true);
        // subTitle.SetActive(false);
        loginObject.SetActive(false);
        customLoginObject.SetActive(false);
        signUpObject.SetActive(false);
        errorObject.SetActive(false);
        nicknameObject.SetActive(false);

        loginField = customLoginObject.GetComponentsInChildren<TMP_InputField>();
        signUpField = signUpObject.GetComponentsInChildren<TMP_InputField>();
        nicknameField = nicknameObject.GetComponentInChildren<TMP_InputField>();

        errorText = errorObject.GetComponentInChildren<Text>();

        loadingObject = GameObject.FindGameObjectWithTag("Loading");
        loadingObject.SetActive(false);
    }

    public void TouchStart()
    {
        // // 업데이트 팝업이 떠있으면 진행 X
        // if (updateObject.activeSelf == true)
        // {
        //     return;
        // }

        //loadingObject.SetActive(true);
        // BackEndServerManager.GetInstance().BackendTokenLogin((bool result, string error) =>
        // {
        //     Dispatcher.Current.BeginInvoke(() =>
        //     {
        //         if (result)
        //         {
        //             ChangeLobbyScene();
        //             return;
        //         }
        //         loadingObject.SetActive(false);
        //         if (!error.Equals(string.Empty))
        //         {
        //             errorText.text = "유저 정보 불러오기 실패\n\n" + error;
        //             errorObject.SetActive(true);
        //             return;
        //         }
                // mainTitle.SetActive(false);
                touchStart.SetActive(false);
                // subTitle.SetActive(true);
                //customLoginObject.SetActive(true);
                loginObject.SetActive(true);
        //     });
        // });
    }

    public void Login()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = loginField[ID_INDEX].text;
        string pw = loginField[PW_INDEX].text;

        if (id.Equals(string.Empty) || pw.Equals(string.Empty))
        {
            errorText.text = "ID 혹은 PW 를 먼저 입력해주세요.";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        BackEndServerManager.GetInstance().CustomLogin(id, pw, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorText.text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }

    public void SignUp()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = signUpField[ID_INDEX].text;
        string pw = signUpField[PW_INDEX].text;

        if (id.Equals(string.Empty) || pw.Equals(string.Empty))
        {
            errorText.text = "ID 혹은 PW 를 먼저 입력해주세요.";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        BackEndServerManager.GetInstance().CustomSignIn(id, pw, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorText.text = "회원가입 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }

    public void ActiveNickNameObject()
    {
        if (this == null) 
        {
            Debug.LogError("ActiveNickNameObject 호출 시 LoginUI 인스턴스가 이미 파괴되었습니다.");
            return;
        }


        Dispatcher.Current.BeginInvoke(() =>
        {
            // mainTitle.SetActive(false);
            touchStart.SetActive(false);
            // subTitle.SetActive(true);
            loginObject.SetActive(false);
            customLoginObject.SetActive(false);
            signUpObject.SetActive(false);
            errorObject.SetActive(false);
            loadingObject.SetActive(false);
            nicknameObject.SetActive(true);
        });
    }

    public void UpdateNickName()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string nickname = nicknameField.text;
        if (nickname.Equals(string.Empty))
        {
            errorText.text = "닉네임을 먼저 입력해주세요";
            errorObject.SetActive(true);
            return;
        }
        loadingObject.SetActive(true);
        BackEndServerManager.GetInstance().UpdateNickname(nickname, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorText.text = "닉네임 생성 오류\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }

    public void GuestLogin()
    {
        if (errorObject.activeSelf)
        {
            return;
        }

        loadingObject.SetActive(true);
        BackEndServerManager.GetInstance().GuestLogin((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorText.text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }

    void ChangeLobbyScene()
    {
        ServerGameManager.GetInstance().ChangeState(ServerGameManager.GameState.MatchLobby);
    }
}
