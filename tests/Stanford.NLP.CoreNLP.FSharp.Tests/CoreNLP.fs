﻿module Stanford.NLP.CoreNLP.CoreNLPTests

open Expecto
open Stanford.NLP.Config
open java.util
open java.io
open edu.stanford.nlp.ling
open edu.stanford.nlp.pipeline
open edu.stanford.nlp.util
open edu.stanford.nlp.io
open edu.stanford.nlp.trees
open edu.stanford.nlp.semgraph


let customAnnotationPrint (annotation:Annotation) =
    printfn "-------------"
    printfn "Custom print:"
    printfn "-------------"
    let sentences = annotation.get(CoreAnnotations.SentencesAnnotation().getClass()) :?> java.util.ArrayList
    Expect.isGreaterThan (sentences.size()) 0 "No sentences found"
    for sentence in sentences |> Seq.cast<CoreMap> do
        printfn "\n\nSentence : '%O'" sentence

        let tokens = sentence.get(CoreAnnotations.TokensAnnotation().getClass()) :?> java.util.ArrayList
        Expect.isGreaterThan (tokens.size()) 0 "No tokens found"
        for token in (tokens |> Seq.cast<CoreLabel>) do
            let word = token.get(CoreAnnotations.TextAnnotation().getClass())
            Expect.isNotNull word "Word not found"
            let pos  = token.get(CoreAnnotations.PartOfSpeechAnnotation().getClass())
            Expect.isNotNull pos "POS not found"
            let ner  = token.get(CoreAnnotations.NamedEntityTagAnnotation().getClass())
            Expect.isNotNull ner "NER not found"
            printfn "%O \t[pos=%O; ner=%O]" word pos ner

        printfn "\nTree:"
        let tree = sentence.get(TreeCoreAnnotations.TreeAnnotation().getClass()) :?> Tree
        Expect.isNotNull tree "Parse Tree is null"
        use stream = new ByteArrayOutputStream()
        tree.pennPrint(new PrintWriter(stream))
        printfn "The first sentence parsed is:\n %O" <| stream.toString()

        printfn "\nDependencies:"
        let deps = sentence.get(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation().getClass()) :?> SemanticGraph
        Expect.isNotNull deps "Semantic graph is null"
        for edge in deps.edgeListSorted().toArray() |> Seq.cast<SemanticGraphEdge> do
            let gov = edge.getGovernor()
            Expect.isNotNull gov "Governor is null"
            let dep = edge.getDependent()
            Expect.isNotNull dep "Dependent is null"
            printfn "%O(%s-%d,%s-%d)"
                (edge.getRelation())
                (gov.word()) (gov.index())
                (dep.word()) (dep.index())

let [<Tests>] coreNlpTests =
    testList "CoreNLP" [
        testCase "StanfordCoreNlpDemo.java that change current directory" <| fun _ ->
            let text = "Kosgi Santosh sent an email to Stanford University. He didn't get a reply.";

            // Annotation pipeline configuration
            let props = Properties()
            props.setProperty("annotators","tokenize, ssplit, pos, lemma, ner, parse, dcoref") |> ignore
            props.setProperty("sutime.binders","0") |> ignore
            props.setProperty("ner.useSUTime","0") |> ignore

            // we should change current directory so StanfordCoreNLP could find all the model files
            let curDir = System.Environment.CurrentDirectory
            System.IO.Directory.SetCurrentDirectory(CoreNLP.jarRoot)
            let pipeline = StanfordCoreNLP(props)
            System.IO.Directory.SetCurrentDirectory(curDir)

            // Annotation
            let annotation = Annotation(text)
            pipeline.annotate(annotation)

            // Result - Pretty Print
            use stream = new ByteArrayOutputStream()
            pipeline.prettyPrint(annotation, new PrintWriter(stream))
            printfn "%O" <| stream.toString()

            customAnnotationPrint annotation

        testCase "StanfordCoreNlpDemo.java with manual configuration" <| fun _ ->
            /// Constants/Keys - https://github.com/stanfordnlp/CoreNLP/blob/1d5d8914500e1110f5c6577a70e49897ccb0d084/src/edu/stanford/nlp/dcoref/Constants.java
            /// DefaultPaths/Values - https://github.com/stanfordnlp/CoreNLP/blob/master/src/edu/stanford/nlp/pipeline/DefaultPaths.java
            /// Dictionaries/Matching - https://github.com/stanfordnlp/CoreNLP/blob/8f70e42dcd39e40685fc788c3f22384779398d63/src/edu/stanford/nlp/dcoref/Dictionaries.java
            let text = "Kosgi Santosh sent an email to Stanford University. He didn't get a reply.";

            // Annotation pipeline configuration
            let props = Properties()
            let (<==) key value = props.setProperty(key, value) |> ignore
            "annotators"    <== "tokenize, ssplit, pos, lemma, ner, parse, dcoref"
            "pos.model"     <== CoreNLP.models "pos-tagger/english-left3words-distsim.tagger"
            "ner.model"     <== CoreNLP.models "ner/english.all.3class.distsim.crf.ser.gz"
            "ner.applyNumericClassifiers" <== "false"
            "ner.useSUTime" <== "false"
            "parse.model"   <== CoreNLP.models "lexparser/englishPCFG.ser.gz"

            "dcoref.demonym"            <== CoreNLP.models "dcoref/demonyms.txt"
            "dcoref.states"             <== CoreNLP.models "dcoref/state-abbreviations.txt"
            "dcoref.animate"            <== CoreNLP.models "dcoref/animate.unigrams.txt"
            "dcoref.inanimate"          <== CoreNLP.models "dcoref/inanimate.unigrams.txt"
            "dcoref.male"               <== CoreNLP.models "dcoref/male.unigrams.txt"
            "dcoref.neutral"            <== CoreNLP.models "dcoref/neutral.unigrams.txt"
            "dcoref.female"             <== CoreNLP.models "dcoref/female.unigrams.txt"
            "dcoref.plural"             <== CoreNLP.models "dcoref/plural.unigrams.txt"
            "dcoref.singular"           <== CoreNLP.models "dcoref/singular.unigrams.txt"
            "dcoref.countries"          <== CoreNLP.models "dcoref/countries"
            "dcoref.extra.gender"       <== CoreNLP.models "dcoref/namegender.combine.txt"
            "dcoref.states.provinces"   <== CoreNLP.models "dcoref/statesandprovinces"
            "dcoref.singleton.predictor"<== CoreNLP.models "dcoref/singleton.predictor.ser"
            //"dcoref.big.gender.number"  <== CoreNLP.models "dcoref/gender.data.gz"
            "dcoref.big.gender.number"  <== CoreNLP.models "dcoref/gender.map.ser.gz"

            //"dcoref.signatures"         <== Models "dcoref/ne.signatures.txt"
            //let dcorefDictionary =
            //    [|
            //        Models.dcoref.``coref.dict1.tsv``
            //        Models.dcoref.``coref.dict2.tsv``
            //        Models.dcoref.``coref.dict3.tsv``
            //        Models.dcoref.``coref.dict4.tsv``
            //    |]
            //"dcoref.dictlist" <== (dcorefDictionary |> String.concat ",")

            let sutimeRules =
                [| CoreNLP.models "sutime/defs.sutime.txt"
                   CoreNLP.models "sutime/english.holidays.sutime.txt"
                   CoreNLP.models "sutime/english.sutime.txt" |]
                |> String.concat ","
            "sutime.rules"      <== sutimeRules
            "sutime.binders"    <== "0"

            let pipeline = StanfordCoreNLP(props)

            // Annotation
            let annotation = Annotation(text)
            pipeline.annotate(annotation)

            // Result - Pretty Print
            use stream = new ByteArrayOutputStream()
            pipeline.prettyPrint(annotation, new PrintWriter(stream))
            printfn "%O" <| stream.toString()

            customAnnotationPrint annotation
    ]
