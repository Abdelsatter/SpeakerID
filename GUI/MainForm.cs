using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Recorder.Recorder;
using Recorder.MFCC;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;
        Sequence seq = null;

        private string path;

        private Encoder encoder;
        private Decoder decoder;

        private bool isRecorded;
        public MainForm()
        {
            InitializeComponent();

            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
            //DBHandler.CreateTables();
        }

        private AudioSignal GetCurrentSignal()
        {
            WaveDecoder waveDecoder = new WaveDecoder(encoder.stream);
            AudioSignal signal = new AudioSignal();
            Signal raw = waveDecoder.Decode(waveDecoder.Frames);

            signal = new AudioSignal
            {
                sampleRate = waveDecoder.SampleRate,
                signalLengthInMilliSec = waveDecoder.Duration,
                data = new double[raw.Samples]
            };

            raw.CopyTo(signal.data);
            return signal;
        }

        private void UpdateSequence()
        {
            AudioSignal enrollSignal = null;

            if (!isRecorded && signal != null)
            {
                enrollSignal = signal;
                enrollSignal = AudioOperations.RemoveSilence(enrollSignal);
            }
            else if (isRecorded && encoder != null)
            {
                encoder.stream.Seek(0, SeekOrigin.Begin);
                enrollSignal = AudioOperations.RemoveSilence(GetCurrentSignal());
            }
            if (enrollSignal == null)
            {
                MessageBox.Show(
                    "No valid audio data found. Please record or open an audio file first.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            seq = AudioOperations.ExtractFeatures(enrollSignal);

            if (seq == null || seq.Frames == null || seq.Frames.Length == 0)
            {
                MessageBox.Show(
                    "Failed to extract features from the audio.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
        }

        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
            UpdateSequence();
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
        }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = true;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = true;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = true;
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                isRecorded = false;
                path = open.FileName;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);
                signal = AudioOperations.RemoveSilence(signal);
                seq = AudioOperations.ExtractFeatures(signal);
                for (int i = 0; i < seq.Frames.Length; i++)
                {
                    for (int j = 0; j < 13; j++)
                    {

                        if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                            throw new Exception("NaN");
                    }
                }
                updateButtons();

            }
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {

            string userName = Interaction.InputBox(
                "Enter the user name for enrollment:",
                "Speaker Enrollment",
                ""
            );
            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("User name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                DBHandler.InsertUserAndAudio(userName, seq);
                MessageBox.Show($"User \"{userName}\" has been enrolled successfully.", "Enrollment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enrolling user: {ex.Message}", "Enrollment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void loadTrain1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Load ll train data
            OpenFileDialog fileDialog = new OpenFileDialog();

            if (match_current_train_chkb.Checked == false) {
                fileDialog.ShowDialog();

                KeyValuePair<string, Sequence>[] ExtractedFeatures = TimingHelper.ExecutionTime(() =>
                {
                    List<User> TrainingData = TestcaseLoader.LoadTestcase1Training(fileDialog.FileName);

                    // Extract features of train data and insert fi el database
                    return FlattenDataAndExtractFeatures(TrainingData);
                }, "Load & Extract Features of Train Set");

                DBHandler.InsertBulkUserAndAudio(ExtractedFeatures);
            }

            // Load ll test data
            fileDialog.ShowDialog();
            Form1 form = new Form1((int W) =>
            {
                Tuple<List<User>, List<string>> result = TimingHelper.ExecutionTime(() =>
                {
                    List<User> data = TestcaseLoader.LoadTestcase1Testing(fileDialog.FileName);
                    // Compare between test data and train data and get accuracy.
                    List<string> predicted = CompareTrainingWithTesting(data, W);

                    return Tuple.Create(data, predicted);
                }, "Load, Extract Features & Match of Test Set");

                double accuracy = TestcaseLoader.CheckTestcaseAccuracy(result.Item1, result.Item2);
                MessageBox.Show($"Accuracy: {(100 - (accuracy * 100)):F2}%", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            form.Show();
        }

        private List<string> CompareTrainingWithTesting(List<User> data, int W = -1)
        {
            // This function compares training data with testing data and gives us the accuracy
            // compare one (or more) of the testing samples with all 

            List<KeyValuePair<string, Sequence>> TrainingData = DBHandler.GetAllAudioFiles();
            if (TrainingData.Count == 0)
                throw new Exception("No Train data please provide the training data through: Edit -> Load Train1");

            KeyValuePair<string, Sequence>[] TestingData = FlattenDataAndExtractFeatures(data);
            string[] predicted = new string[TestingData.Length];

            Parallel.For(0, TestingData.Length, i =>
            {
                var testSample = TestingData[i];
                Sequence testFeatures = testSample.Value;

                string user = null;
                double minDistance = double.MaxValue;

                foreach (var trainSample in TrainingData)
                {
                    double distance = DTW.ComputeDTW(testFeatures, trainSample.Value, W);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        user = trainSample.Key;
                    }
                }

                predicted[i] = user;
            });

            return predicted.ToList();
        }

        private KeyValuePair<string, Sequence> ExtractFeaturesOfData(KeyValuePair<string, AudioSignal> user)
        {
            Sequence currentSequence = AudioOperations.ExtractFeatures(user.Value);
            return new KeyValuePair<string, Sequence>(user.Key, currentSequence);
        }

        private KeyValuePair<string, Sequence>[] FlattenDataAndExtractFeatures(List<User> data)
        {
            KeyValuePair<string, AudioSignal>[] DataBag = data.SelectMany(user =>
                user.UserTemplates.Select(template =>
                    new KeyValuePair<string, AudioSignal>(user.UserName, template)
                )
            ).ToArray();

            KeyValuePair<string, Sequence>[] ExtractedFeatures = new KeyValuePair<string, Sequence>[DataBag.Length];

            Console.WriteLine($"Samples Count: {DataBag.Length}");

            //var partitioner = Partitioner.Create(0, DataBag.Length);
            //Parallel.ForEach(partitioner, range =>
            //{
            //    for (int i = range.Item1; i < range.Item2; i++)
            //        ExtractedFeatures[i] = ExtractFeaturesOfData(DataBag[i]);
            //});

            //Parallel.For(0, DataBag.Length, i =>
            //    ExtractedFeatures[i] = ExtractFeaturesOfData(DataBag[i])
            //);

            // only one thread can enter the critical section of MATLAB function at a time.
            for (int i = 0; i < DataBag.Length; i++) ExtractedFeatures[i] = ExtractFeaturesOfData(DataBag[i]);

            return ExtractedFeatures;
        }


        private void Identify(int w)
        {
            double minDistance = double.MaxValue;
            string userName = "";
            var templates = DBHandler.GetAllAudioFiles();

            DBHandler.PrintUserSequenceCounts();
            double distance;
            foreach (var user in templates)
            {
                Console.WriteLine("User: " + user.Key);

                if (w == -1)
                    distance = TimingHelper.ExecutionTime(() => DTW.ComputeDTW(seq, user.Value), "ComputeDTW");
                else
                    distance = TimingHelper.ExecutionTime(() => DTW.ComputeDTW(seq, user.Value, w), "ComputeDTW");

                Console.WriteLine("Distance: " + distance);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    userName = user.Key;
                }
            }

            MessageBox.Show($"Identified user: {userName}", "Identification Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return;
        }
        private void btnIdentify_ClickAsync(object sender, EventArgs e)
        {
            Form1 form1 = new Form1(Identify);
            form1.Show();

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void reset_tbl_btn_Click(object sender, EventArgs e)
        {
            DBHandler.ResetTables();
            MessageBox.Show("Tables were resetted properly.", "Database Reset", MessageBoxButtons.OK);
        }
    }
}
