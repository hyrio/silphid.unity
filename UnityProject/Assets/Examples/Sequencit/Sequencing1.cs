﻿﻿using System;
using Silphid.Extensions;
using Silphid.Sequencit;
using Silphid.Tweenzup;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class Sequencing1 : MonoBehaviour
{
    // Connections

    public Button StartButton;
    public Button CancelButton;
    public Text Text;
    public GameObject Cube;

    // Configs

    public float RotateDuration = 0.5f;
    public float MoveDuration = 0.4f;
    public float ShowHideTextDuration = 0.4f;
    public float FakeLoadDuration = 3.4f;
    public Vector3 NormalCubePosition;
    public Vector3 LoadingCubePosition;
    public Vector3 NormalTextPosition;
    public Vector3 LoadingTextPosition;

    // Private fields

    // SerialDisposables always dispose their previous disposable when assigning them a new value.
    private readonly SerialDisposable _serialDisposable = new SerialDisposable();

    // ReactiveProperties are observables that store their current value and allow that value to be set.
    private readonly BoolReactiveProperty _isLoading = new BoolReactiveProperty();
    private readonly BoolReactiveProperty _isCancelling = new BoolReactiveProperty();

	internal void Awake()
	{
        // Bind buttons

        // Observables that control whether the buttons are interactable or not.
	    var canStart = _isLoading.Not();
	    var canCancel = _isLoading.And(_isCancelling.Not());

        // BindTo() is a Silphid extension that subscribes the button to a canExecute observable (that determines
        // whether the button is interactable or not) and to its click event/observable (in order to trigger an action).
        // It then returns an IDisposable that can be used to remove the binding, which we then AddTo(this) (so that
        // it gets automatically disposed when this component is destroyed). 
        StartButton.BindTo(canStart, StartLoading).AddTo(this);   
        CancelButton.BindTo(canCancel, CancelLoading).AddTo(this);
	}

    private void StartLoading()
    {
        _isLoading.Value = true;

        // Sequence.Start() creates a new sequence and passes it to the lambda expression, so we can add steps to it, and then
        // immediately subscribes to the sequence, which triggers its execution.  That's why it returns an IDisposable,
        // to allow to unsubscribe from (cancel) the sequence.  Because we reuse the same SerialDisposable for starting and cancelling,
        // it handles disposing the previous sequence (or cancelling it, if it is still executing).
        _serialDisposable.Disposable =
            Sequence.Start(seq =>
            {
                // AddParallel(...) is a shorthand for Add(Parallel.Create(...)). There are multiple overloads of this method, but
                // this is the most compact, when all your methods are returning observables.  Note that we are using the Method Group
                // syntax for even more compact code. For example, instead of passing a "() => ShowText()" lambda, which has the same
                // signature as ShowText(), we can only specify the "ShowText" method group, without any parenthesis.  The important
                // thing to remember is that the ShowText() method is *not* invoked immediately here, exactly as for lambdas.
                seq.AddParallel(MoveCubeToLoadingPosition, ShowText);

                // The TakeUntil() Rx operator allows to start both the RotateCubeIndefinitely() and LoadGreeting() operations,
                // but it terminates the infinite animation as soon as LoadGreeting() emits a value or completes. The Add() extension
                // method supports any type T for IObservable<T>, but it disregards all emitted values, so it is our responsability
                // to act upon meaningful values, as we are doing here with the Do() Rx operator to log the loaded greeting as a
                // side-effect.
                seq.Add(RotateCubeIndefinitely()
                    .Until(LoadGreeting()
                        .Do(x => Debug.Log($"Greeting loaded: {x}"))));

                // This is a more verbose, but much more flexible overload of AddParallel(), because it passes the new parallel
                // object to the lambda and you therefore have access to all its extensions methods (like AddSequence() in this case).
                seq.AddParallel(p =>
                {
                    p.AddSequence(s =>
                    {
                        s.Add(ResetCubeRotation);
                        s.AddDelay(0.25f);
                        s.Add(MoveCubeToNormalPosition);
                    });

                    p.Add(HideText);
                });

                // Actions are instantaneous steps in a sequence and, as such, do not return an observable.
                // They are very handy when synchronous code must be executed at a specific point in a sequence.
                seq.AddAction(Complete);
            });
    }

    private void CancelLoading()
    {
        _isCancelling.Value = true;

        _serialDisposable.Disposable =
            Sequence.Start(seq =>
            {
                seq.AddParallel(MoveCubeToNormalPosition, ResetCubeRotation, HideText);
                seq.AddAction(Complete);
            });
    }

    // This is just a method that fakes an asynchronous loading operation.  In a real application, such a method might load a level,
    // some prefabs or assets from the web. Notice that this method's body is specified after a => operator and without a "return"
    // keyword nor braces. This is the "expression body" syntax introduced in C# 6.0, which can also be used for declaring properties.
    private IObservable<string> LoadGreeting() =>
        Observable
            .Timer(TimeSpan.FromSeconds(FakeLoadDuration))
            .Select(_ => "Hello World!");

    private void Complete()
    {
        _isLoading.Value = false;
        _isCancelling.Value = false;
    }

    // Rotate cube

    // Sequence.Create() creates a new sequence and allows to add steps to it but, as opposed to Sequence.Start(), it only returns
    // it without subscribing to it.  It is the responsability of the caller to subscribe to it, or to pass it up the call chain.
    // NOTE: It is a very important best practice to preserve the chaining of your observables as much as possible and to avoid
    // breaking that chain with calls to Subscribe().  As much as possible/reasonable, try to defer the call to Subscribe() to
    // callers up the chain.  That ensures errors can always bubble up to higher level functions and also that disposing the chain
    // at a higher level will dispose it completely.
    private ICompletable RotateCubeIndefinitely() =>
        Sequence
            .Create(
                () => RotateCube(Vector3.up * 180),
                () => RotateCube(Vector3.right * 180),
                () => RotateCube(Vector3.forward * 180))
            .Repeat();

    // Disposing the returned completable has the effect of unterrupting the underlying Tween
    private ICompletable RotateCube(Vector3 angle) =>
        Cube.transform.RotateTo(angle, RotateDuration, Easer.InOutCubic);

    private ICompletable ResetCubeRotation() =>
        Cube.transform.RotateTo(Vector3.zero, RotateDuration, Easer.InOutCubic);

    // Move cube

    private ICompletable MoveCubeToLoadingPosition() => MoveCubeTo(LoadingCubePosition);
    private ICompletable MoveCubeToNormalPosition() => MoveCubeTo(NormalCubePosition);
    private ICompletable MoveCubeTo(Vector3 position) =>
        Cube.transform.MoveTo(position, MoveDuration, Easer.InOutCubic);

    // Show or hide text

    private ICompletable ShowText() => ShowHideText(LoadingTextPosition, 1);
    private ICompletable HideText() => ShowHideText(NormalTextPosition, 0);
    private ICompletable ShowHideText(Vector3 position, float alpha) =>
        Parallel.Create(
            () => Text.GetComponent<CanvasGroup>().FadeTo(alpha, ShowHideTextDuration, Easer.InOutCubic),
            () => Text.transform.MoveTo(position, ShowHideTextDuration, Easer.InOutCubic));
}
