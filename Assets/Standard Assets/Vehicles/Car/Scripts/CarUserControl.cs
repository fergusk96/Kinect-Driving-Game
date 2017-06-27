using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private KinectWrapper.NuiSkeletonPositionIndex TrackedHead = KinectWrapper.NuiSkeletonPositionIndex.Head;
        private KinectWrapper.NuiSkeletonPositionIndex TrackedHandL = KinectWrapper.NuiSkeletonPositionIndex.HandLeft;
        private KinectWrapper.NuiSkeletonPositionIndex TrackedHandR = KinectWrapper.NuiSkeletonPositionIndex.HandRight;
        private KinectWrapper.NuiSkeletonPositionIndex TrackedBody = KinectWrapper.NuiSkeletonPositionIndex.Spine;
        private KinectWrapper.NuiSkeletonPositionIndex TrackedKneeL = KinectWrapper.NuiSkeletonPositionIndex.KneeLeft;

        private CarController m_Car; // the car controller we want to use
    
        float h;

        private float kneeToBody = 0.0f;
        private float userPos = 0.0f;
        private bool jump;
        private float v;
       
        private float period = 0.0f;
        private float handbrake;
        private Boolean changed = true;
        private Boolean squating;
        private float distance;
        private float originalHeadKnee = 0.0f;
        private bool speedBoost = false;
        private bool exploder = false;
        private bool cubes;
        private int droppedCubes = 1;
        private GameObject rearWheel;
        private bool leftHandUp1;
        private bool leftHandDown1;
        private bool rightHandUp1;
        private bool rightHandDown1;
        private int playersCompletedLap = 0;
        private bool toggleGUI;
        public bool isPlayer1;
        private SerialPort sp;



        private void Awake()
        {
            // get the car controller\leftHandUp1 = false;
            if (isPlayer1) {
                sp = new SerialPort ("COM8", 19200);
            } 
            else {  
                sp = new SerialPort ("COM7", 19200);        
            }
            leftHandDown1 = false;
            rightHandDown1 = false;
            rightHandUp1 = false;
            toggleGUI = false;
            rearWheel = GameObject.FindGameObjectWithTag("RearWheel");
            m_Car = GetComponent<CarController>();
            cubes = true;
            sp.Open();
            sp.ReadTimeout = 5;
            h = 0;

        }

        

        private void FixedUpdate() { 
        

            KinectManager manager = KinectManager.Instance;
            if ((!manager || !manager.IsInitialized()))
                return;

            if ((!manager.IsUserDetected()))
                return;

            
            int iJointHead = (int)TrackedHead; //Tracking each relevant body part
            int iJointBody = (int)TrackedBody;
            int iJointHandL = (int)TrackedHandL;
            int iJointHandR = (int)TrackedHandR;
            int iJointKneeL = (int)TrackedKneeL;

            uint userId;

            if (isPlayer1)
            {

                userId = manager.GetPlayer1ID();
                
            }
            else { userId = manager.GetPlayer2ID(); }


            

            Vector3 posJointHandL = manager.GetRawSkeletonJointPos(userId, iJointHandL);
            Vector3 posJointHandR = manager.GetRawSkeletonJointPos(userId, iJointHandR);
            Vector3 posJointHead = manager.GetRawSkeletonJointPos(userId, iJointHead);
            Vector3 posJointBody = manager.GetRawSkeletonJointPos(userId, iJointBody); //Get Head Position
            Vector3 posJointKneeL = manager.GetRawSkeletonJointPos(userId, iJointBody);
            float headKnee = GetJointDistance(posJointHead.x, posJointHead.y, posJointKneeL.x, posJointKneeL.y);
            if (userPos == 0.0f || (changed == false && period > 0.3f)) {
                userPos = manager.GetUserPosition(userId).y;
                changed = true;
            }
            if (originalHeadKnee == 0.0)
            {
                originalHeadKnee = headKnee;
            }
            if (manager.GetUserPosition(userId).y > (userPos + (0.10f))) {
                jump = true;
                changed = false;
                period = 0;

            }
            
            if (headKnee < (originalHeadKnee - (0.03)) && v >= -1.0f && headKnee != 0.0f)
            {
                m_Car.SetSquat(true);
                v -= (0.1f);

            }


            else if (headKnee >= (originalHeadKnee - (0.03)) && v <= 1.0f && headKnee != 0.0f)
            {
                m_Car.SetSquat(false);
                v += (0.2f);
            }
            

            if (Math.Abs(posJointHead.y - posJointHandR.y) < 0.15 && (GetJointDistance(posJointHead.x, posJointHead.y, posJointHandR.x, posJointHandR.y) > 0.6))
            {
                
                leftHandUp1 = true;
               
            }
            if (leftHandUp1 && (!(Math.Abs(posJointHead.y - posJointHandR.y) < 0.15) && !(GetJointDistance(posJointHead.x, posJointHead.y, posJointHandR.x, posJointHandR.y) > 0.6)))
            {
                leftHandDown1 = true;
                

            }
            
            if (leftHandDown1 && leftHandUp1)
            {
                
                Debug.Log("HandsUp");
            }
            if (Math.Abs(posJointHead.y - posJointHandL.y) < 0.15 && (GetJointDistance(posJointHead.x, posJointHead.y, posJointHandL.x, posJointHandL.y) > 0.6))
            {

                rightHandUp1 = true;

            }
            if (rightHandUp1 && (!(Math.Abs(posJointHead.y - posJointHandL.y) < 0.15) && !(GetJointDistance(posJointHead.x, posJointHead.y, posJointHandL.x, posJointHandL.y) > 0.6)))
            {
                rightHandDown1 = true;


            }

            if (rightHandDown1 && rightHandUp1)
            {
                        Debug.Log("HandsUp");
            }

                        // pass the input to the car!
            try
            {
               h = float.Parse(sp.ReadLine());

                

            }
            catch (Exception e) {
                
                
            }
            period += Time.deltaTime;

#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            
            m_Car.Move(h, v, v, handbrake,jump,speedBoost,exploder);
            speedBoost = false;
            exploder = false;
            jump = false;
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }

        private float GetJointDistance(float x1, float y1, float x2, float y2)
        {

            distance = (float)Math.Pow(x2 - x1, 2) + (float)Math.Pow(y2 - y1, 2);
            distance = (float)Math.Sqrt(distance);
            return distance;
        }
      
    }

}
