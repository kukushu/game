using UnityEngine;

public class Molotov : MonoBehaviour
{
	public Rigidbody rb;

	private int LifeSpan = 1020;

	private bool hasExploded;

	private bool stuffToDo;

	private BlockData b;

	public ParticleSystem Ps_Splash;

	private MeshRenderer mh;

	public AudioSource explosionGlass;

	public AudioSource explosionGlassSmallA;

	public AudioSource explosionGlassSmallB;

	public World w;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		mh = GetComponent<MeshRenderer>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		Vector3 position = gameObject.transform.position;
		b = w.GetBounded((int)position.x, (int)position.z);
		if (b.lO != null && !stuffToDo)
		{
			stuffToDo = true;
			explosionGlass.Play();
		}
		else if (Random.value < 0.5f)
		{
			explosionGlassSmallA.Play();
		}
		else
		{
			explosionGlassSmallB.Play();
		}
		if (!hasExploded && LifeSpan < 800)
		{
			Debug.Log("Destroy FX");
			hasExploded = true;
			mh.enabled = false;
			Ps_Splash.Play();
			Invoke("DestroyFx", 4f);
		}
	}

	private void DestroyFx()
	{
		Object.Destroy(gameObject);
	}

	private void FixedUpdate()
	{
		LifeSpan--;
		if (stuffToDo && b.lO != null && !b.lO.isDecayed && b.lO.Stage > 0)
		{
			LotCreator lO = b.lO;
			stuffToDo = false;
			lO.decayReason = "Looted by riots";
			Debug.Log("Riot Decays LC! " + lO.name);
			lO.DecayLot(toParking: false);
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Construction/Fire"));
			gameObject.transform.position = new Vector3(lO.Anchor.x, lO.wP.y + 0.504f, lO.Anchor.z);
			FireCoroutine component = gameObject.GetComponent<FireCoroutine>();
			component.lotOnFire = lO;
			foreach (ServiceCreator service in w.Services)
			{
				if (service.type == ServiceCreator.Type.Fire && service.carsOut < service.Stage)
				{
					w.indexer.zoning.lm.traffic.SendSpecialVehicleOnPath(service, lO, "Vehicles/FireEngine", component);
					return;
				}
			}
		}
		if (LifeSpan < 0)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
