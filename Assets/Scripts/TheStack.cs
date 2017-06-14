using UnityEngine;
using System.Collections;

public class TheStack : MonoBehaviour {
    // Stałe
    // Ograniczenie wielkości naszej platformy
    private const float BOUNDS_SIZE = 4;
    // Prędkość przesuwania stosu w dół
    private const float STACK_MOVING_SPEED = 5.0f;
    // Margines błędu przy stawianiu platformy
    private const float ERROR_MARGIN = 0.1f;
    // Stała określająca przyrost wielkości cuba w czasie combo
    private const float STACK_BOUNDS_GAIN = 0.25f;
    // Ilość idealnych dopasowań potrzebnych do zadziałania combo
    private const int COMBO_START_GAIN = 5;

    // Tablica cubów
    private GameObject[] theStack;
    // Zmienna przechowująca aktualną wielkość x i z
    private Vector2 stackBounds = new Vector2(BOUNDS_SIZE, BOUNDS_SIZE);

    // Index ostatniego elementu na stosie
    private int stackIndex;
    // Licznik wyniku
    private int scoreCount = 0;
    // licznik combo
    private int combo = 0;

    // Przesunięcie platformy wzdłóż osi
    private float tileTransition = 0.0f;
    // Prędość poruszania się cuba
    private float tileSpeed = 2.5f;
    // 
    private float secondaryPosition;

    // Czy porusza się po osi X
    private bool isMovingOnX = true;
    // Czy gra się zakończyła
    private bool gameOver = false;

    // Porządana pozycja x,y,z
    private Vector3 desiredPositon;
    // Pozycja poprzedniego tile
    private Vector3 lastTilePosition;

    AdManager ad;

    // Funckja wywołująca się po stworzeniu obiektu.
    void Start() {
        // Tworzymy tablice theStack o wielkości równej ilości cubów.
        theStack = new GameObject[transform.childCount];
        // Umieszczamy w tablicy theStack wszystkie dzieci Gameobject Stack 
        for(var i = 0; i < transform.childCount; i++) {
            theStack[i] = transform.GetChild(i).gameObject;
        }
        // Do zmienej stackIndex przypisujemy ilość dzieci - 1 co da nam najniższy element
        stackIndex = transform.childCount - 1;
        ad = new AdManager();
    }

    // Funkcja wywołująca się co klatkę
    void Update() {
        // Sprawdzenie czy gracz wcisnął lewy przycisk myszy
        if(Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) {
            // Sprawdzenie czy graczowi udało się ustawić tile
            if(PlaceTile()) {
                // Utworzenie nowej platformy o jeden wyżej
                SpawnTile();
                // Zwiększenie wyniku
                scoreCount++;
            }
            // Zakończenie gry
            else EndGame();
        }
        // Wywołanie funkcji poruszajacej platformą 
        MoveTile();
        // Wywołanie funkcji przesuwającej stos w dół
        MoveStackDown();
    }

    // Funckja przesuwająca stos w dół
    private void MoveStackDown() {
        // Do pozycji stosu przypisujemy funkcje Lerp, przesuwając stos od aktualnej pozycji do porządanej pozycji.
        transform.position = Vector3.Lerp(transform.position, desiredPositon, STACK_MOVING_SPEED*Time.deltaTime);
    }

    // Funckja tworząca odpadającą część platformy. pos - scale -
    private void CreateRubble(Vector3 pos, Vector3 scale) {
        // Utworzenie nowej platformy (o wymiarach 1x1x1)
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Przypisanie pozycji pos
        go.transform.localPosition = pos;
        // Przypisanie skali scale
        go.transform.localScale = scale;
        // Dodanie elementu Rigidbody, które posiada grawitacje
        go.AddComponent<Rigidbody>();
    }

    // Funckja tworząca platforme
    private void SpawnTile() {
        // Przypisanie górnego elementu stosu do zmienej lastTilePosition
        lastTilePosition = theStack[stackIndex].transform.localPosition;

        // Zmniejsznie indexu stosu o jeden
        stackIndex--;
        // Sprawdzenie czy index przeszedł do pierwszego elementu (początku stosu)
        if(stackIndex < 0) {
            // Przypisanie ostatniego elementu stosu
            stackIndex = transform.childCount - 1;
        }
        // Przypisanie pozycji o jeden niższej do porządanej pozycji (wykorzystywana do przesuwania stosu w dół)
        desiredPositon = (Vector3.down)*scoreCount;
        // utworzenie nowej platformy na samej górze.
        theStack[stackIndex].transform.localPosition = new Vector3(0, scoreCount, 0);
        // Przeskalowanie platformy do aktualnych wymiarów
        theStack[stackIndex].transform.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);
    }

    // Funckja przesuwająca platformę wzdłóż osi x i z
    private void MoveTile() {
        // W przypadku gdy gra się zakończyła, wychodzimy z funckji.
        if(gameOver) return;
        // Zwiększnie przesunięcia o czas*prędkość animacji
        tileTransition += Time.deltaTime*tileSpeed;
        // Sprawdzenie czy poruszamy się wzdłóż osi X
        if(isMovingOnX) {
            // Sin daje nam wynik między -1 i 1. 
            // tile transition zmienia się co klatkę dlatego funckja Sin pozwala na utworzenie animacji
            // Tutaj nastepuje przesunięcie platformy wzdłóż osi X
            theStack[stackIndex].transform.localPosition = new Vector3(Mathf.Sin(tileTransition)*BOUNDS_SIZE, scoreCount,
                secondaryPosition);
        }
        else {
            // Identycznie jak w przypadku osi X, tylko na osi Y
            theStack[stackIndex].transform.localPosition = new Vector3(secondaryPosition, scoreCount,
                Mathf.Sin(tileTransition)*BOUNDS_SIZE);
        }
    }

    // Funckja umieszczająca platformę
    private bool PlaceTile() {
        // Tworzymy lokalną zmienną t do której przypisujemy transform górnego elementu stosu
        Transform t = theStack[stackIndex].transform;

        // Sprawdzamy po jakiej osi się poruszamy
        if(isMovingOnX) {
            // Obliczamy różnicę między pozycją poprzedniej platformy a aktualną.
            var deltaX = lastTilePosition.x - t.position.x;
            // Sprawdzamy czy różnica jest większa niż nasz margines błędu
            if(Mathf.Abs(deltaX) > ERROR_MARGIN) {
                // Warunek zwrócił true, zerujemy licznik combo
                combo = 0;
                // zmienijszamy rozmiar platformy o różnicę na osi X
                stackBounds.x -= Mathf.Abs(deltaX);
                // jesli rozmiar naszej platformy po odjęciu różnicy jest mniejszy niż 0 zwracamy false
                if(stackBounds.x <= 0)
                    return false;

                // Obliczamy środek między poprzednią platformą a środkiem aktualnej platformy
                var middle = lastTilePosition.x + t.localPosition.x/2;
                // Zmieniamy skale platform do odpowiednich rozmiarów
                t.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);
                // Tworzymy odpadający kawałek
                CreateRubble(
                    // Przekazujemy pozycje dla kawałka który ma odpaść
                    new Vector3(
                        (t.position.x > 0) ? t.position.x + (t.localScale.x/2) : t.position.x - (t.localScale.x/2),
                        t.position.y,
                        t.position.z),
                    // Oraz skale
                    new Vector3(Mathf.Abs(deltaX), 1, t.localScale.z)
                );
                // Zmieniamy pozycje platformy
                t.localPosition = new Vector3(middle - (lastTilePosition.x/2), scoreCount, lastTilePosition.z);
            }
            else {
                // Sprawdzamy czy ilość combo jest wystarczająca do bonusu
                if(combo > COMBO_START_GAIN) {
                    // Wywołujemy funkcję zwiększająca wielkość platformy
                    IncreaseBouncsOnXAxis(t);
                }
                // Zwiększenie licznika combo
                combo++;
                // Przypisanie wielkości poprzedniej platformy. 
                t.localPosition = new Vector3(lastTilePosition.x, scoreCount, lastTilePosition.z);
            }
        }
        else {
            var DeltaZ = lastTilePosition.z - t.position.z;
            if(Mathf.Abs(DeltaZ) > ERROR_MARGIN) {
                combo = 0;
                stackBounds.y -= Mathf.Abs(DeltaZ);
                if(stackBounds.y <= 0)
                    return false;

                var middle = lastTilePosition.z + t.localPosition.z/2;
                t.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);
                CreateRubble(
                    new Vector3(
                        t.position.x,
                        t.position.y,
                        (t.position.z > 0) ? t.position.z + (t.localScale.z/2) : t.position.z - (t.localScale.z/2)),
                    new Vector3(Mathf.Abs(DeltaZ), 1, t.localScale.z)
                );
                t.localPosition = new Vector3(lastTilePosition.x, scoreCount, middle - (lastTilePosition.z/2));
            }
            else {
                if(combo > COMBO_START_GAIN) {
                    IncreaseBoundsOnZAxis(t);
                }
                combo++;
                t.localPosition = new Vector3(lastTilePosition.x, scoreCount, lastTilePosition.z);
                ;
            }
        }

        secondaryPosition = (isMovingOnX)
            ? t.localPosition.x
            : t.localPosition.z;
        isMovingOnX = !isMovingOnX;
        return true;
    }

    private void IncreaseBoundsOnZAxis(Transform tr) {
        stackBounds.y += STACK_BOUNDS_GAIN;
        if(stackBounds.y > BOUNDS_SIZE) {
            stackBounds.y = BOUNDS_SIZE;
        }
        var middle = lastTilePosition.z + tr.localPosition.z / 2;
        tr.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);
        tr.localPosition = new Vector3(lastTilePosition.x, scoreCount, middle - (lastTilePosition.z / 2));
    }

    private void IncreaseBouncsOnXAxis(Transform tr) {
        stackBounds.x += STACK_BOUNDS_GAIN;
        if(stackBounds.x > BOUNDS_SIZE) {
            stackBounds.x = BOUNDS_SIZE;
        }
        var middle = lastTilePosition.x + tr.localPosition.x / 2;
        tr.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);
        tr.localPosition = new Vector3(middle - (lastTilePosition.x / 2), scoreCount, lastTilePosition.z);
    }

    private void EndGame() {
        Debug.Log("GameOver");
        gameOver = true;
        theStack[stackIndex].AddComponent<Rigidbody>();
        ad.ShowRewardedAd();        
    }
}
