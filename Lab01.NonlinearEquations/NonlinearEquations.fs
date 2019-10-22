namespace NumericalMethods

module NonlinearEquations = 
    open System
    open Utils
    open Utils.Segment

    type Methods = 
        | Bisection   
        | Newton of (float -> float)
        | ModifiedNewton of (float -> float)
        | Secant

    type MethodInfo = {
        mutable Segment: LineSegment
        mutable StartApproximation: float option
        mutable StepCount: int
        mutable Root: float
        mutable AbsoluteError: float
    } 
    with 
        static member Default = {
            Segment = LineSegment.Default
            StartApproximation = None
            StepCount = 0
            Root = 0.
            AbsoluteError = 0.
        }

    let separateRoots f stepSize section = 
        splitSegmentWithStep stepSize section
        |> Seq.filter (fun x -> (f <| x.Left) * (f <| x.Right) <= 0.)

    let findRoots (f: float -> float) (section: LineSegment) (epsilon: float) (method: Methods) =
        let bisectionMethod startApproximation = 
            let rec loop currentApproximation stepNumber = 
                let middle = (currentApproximation.Right + currentApproximation.Left) / 2.
                if Math.Abs (currentApproximation.Right - currentApproximation.Left) < 2. * epsilon then 
                    middle, (stepNumber + 1)
                else 
                    if (f currentApproximation.Left) * (f middle) < 0. then 
                        loop {Left = currentApproximation.Left; Right = middle} (stepNumber + 1)
                    else
                        loop {Left = middle; Right = currentApproximation.Right} (stepNumber + 1)
            loop startApproximation 0

        let newtonMethod startApproximation f' = 
            let rec loop currentApproximation multiplicityFactor stepNumber = 
                try 
                    let nextApproximation = 
                        currentApproximation - 
                        multiplicityFactor * (f currentApproximation) / (f' currentApproximation)
                    if Math.Abs (nextApproximation - currentApproximation) <= epsilon then 
                        nextApproximation, (stepNumber + 1)
                    else
                        loop nextApproximation multiplicityFactor (stepNumber + 1)
                with 
                | :? DivideByZeroException -> 
                    loop currentApproximation (multiplicityFactor + 2.) stepNumber
            loop startApproximation 1. 0

        let newtonModifiedMethod startApproximation f' = 
            let derivative = f' startApproximation
            let rec loop currentApproximation stepNumber = 
                let nextApproximation = 
                    currentApproximation - (f currentApproximation) / derivative
                if Math.Abs (nextApproximation - currentApproximation) <= epsilon then 
                    nextApproximation, (stepNumber + 1)
                else
                    loop nextApproximation (stepNumber + 1)
            loop startApproximation 0

        let secantMethod startApproximation'1 startApproximation'2 = 
            let rec loop currentApproximation previousApproximation stepNumber = 
                let nextApproximation = 
                    currentApproximation - 
                    (f currentApproximation) * (currentApproximation - previousApproximation) / 
                    ((f currentApproximation) - (f previousApproximation))
                if Math.Abs (nextApproximation - currentApproximation) <= epsilon then 
                    nextApproximation, (stepNumber + 1)
                else
                    loop nextApproximation currentApproximation (stepNumber + 1)
            loop startApproximation'1 startApproximation'2 0

        section
        |> separateRoots f 1e-2
        |> Seq.map 
            (fun section -> 
                let middle = (section.Left + section.Right) / 2.
                let logAndReturn startApproximation compMethodResult = 
                    let logger = MethodInfo.Default
                    logger.Segment <- section
                    logger.StartApproximation <- startApproximation
                    logger.StepCount <- snd compMethodResult
                    logger.Root <- fst compMethodResult
                    logger.AbsoluteError <- Math.Abs (f <| fst compMethodResult)
                    fst compMethodResult, logger

                match method with
                | Bisection -> logAndReturn None <| bisectionMethod section
                | Newton derivative -> logAndReturn (Some middle) <| newtonMethod middle derivative
                | ModifiedNewton derivative -> logAndReturn (Some middle) <| newtonModifiedMethod middle derivative
                | Secant -> logAndReturn None <| secantMethod section.Left section.Right
            )
        |> Seq.toList
        |> List.unzip