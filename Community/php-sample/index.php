<?php

	/*
	*	Vocaluxe Community feature test
	*/
	
	
	//JSON response function
	function Response($r) {
		if (is_array($r)) {
			$response = json_encode($r);
		}
		$r = $response;
		header("Content-Type: application/json");
		header("Content-Lenght: ".strlen($response));
		header("X-Vocaluxe-Api-Version: 0.1.1");
		exit($response);
	}
	
	//we wait for post request
	if ($_SERVER['REQUEST_METHOD'] == 'POST') {
		if (isset($HTTP_RAW_POST_DATA)) {
			if (($request = @json_decode($HTTP_RAW_POST_DATA,true)) === NULL) {
				Response(Array('status'=>0, 'code'=>1, 'message'=>'JSON decode failed!'),true);
			}
			
			if (isset($request['method'])) {
			
				/*
				* Vocaluxe send scores to server */
				if ($request['method'] == 'setscores') {
					/*	
						Incoming data:
						$request = Array(
							#Required data
							'version' 		=> Vocaluxe client version (in x.x.x version format)
							'method'		=> Method (string)
							'gameMode'		=> Vocaluxe gamemode (string) (TR_GAMEMODE_NORMAL,TR_GAMEMODE_MEDLEY,TR_GAMEMODE_DUET,TR_GAMEMODE_SHORTSONG)
							
							#Important: If a player has no community profile, their scores will be ignored. Community profiles can be selected on profile screen. 
							'scores'		=> Array()
								'username'		=> Username (string) 
								'password'  	=> Password (string) 
								'playerName'	=> Player Vocaluxe profile name (string)
								'playerId'		=> Player Vocaluxe id (int)
								'score'			=> final score (double)  (final score = goldenbonus + linebonus + earned score)
								'goldenbonus'	=> golden bonus (double)
								'linebonus'		=> line bonus (double)
								'gameMode'		=> unused
								'round'			=> round number (int)
								'difficulty'	=> (int)
								'txthash'		=> Md5 hash of song (string) (explained later)
								'artist'		=> Artist (string) 
								'title'			=> Song title (string) 
								'voicenr'		=> in duet mode (int) 0,1	
							
							 #Optional data
							'guests'		=> number of guest players (int)
											   Guest players means players without community profile
							'partyMode'		=> Party mode name (string)
											
						);
					*/
				
					if (count($request['scores'])) {
						//authentication and save scores to database ...
							
						//save to file
						file_put_contents("incoming.score.txt", print_r($request,true));
						//test case: Random status
						if (rand(0,1) == 0) {
							Response(Array('status'=>0, 'code'=>13, 'message'=>"Custom error message for this request.\nYou could use newlines!"));
						}else {
							Response(Array('status'=>1, 'code'=>0, 'message'=>"Saved!"));
						}
						
					}
					
					Response(Array('status'=>0, 'code'=>4, 'message'=>'Custom error message. Vocaluxe can display it.'));
					
				/*
				*  Get highscores for a song */	
				
				}elseif ($request['method'] == 'getscores') {
					/*	
						Incoming data:
						$request = Array(
							#Required data
							'version' 		=> Vocaluxe client version (in x.x.x version format)
							'method'		=> Method (string)
							'txthash'		=> MD5 hash of txt file (string) (explained later)
							
							 #Optional data
							'username'		=> Username (string) Currently not send it. Scores can be loaded without authentication
							'password'		=> Password (string)
							'gameMode'		=> Vocaluxe gamemode (string) (TR_GAMEMODE_NORMAL,TR_GAMEMODE_MEDLEY,TR_GAMEMODE_DUET,TR_GAMEMODE_SHORTSONG)
											   If present, only need to send scores for this gamemode
							
							'partyMode'		=> Party mode name (string)
											   If present, only need to send scores for this partymode (and gamemode)
							'difficulty'	=> (int) (1:Easy,2:Medium,3:Hard), 
											   If present, only need to send scores for this difficulty, otherwise need to send all three difficulties
											   
						    'queryType'		=> Currently unused, Using later in Community Contest party mode (string)
							'id'			=> Currently unused, Using later in Community Contest party mode (int) 
											
						);
					*/
					
					//test response 
					
					$scores = Array(
						'lastrefresh' => time(), 	//last scoredb update 
						'easy'		=> Array(),		//score array for easy
						'medium'	=> Array(),		//score array for medium
						'hard'		=> Array()		//score array for hard
					);
					
					if (isset($request['txthash'])) {
						//test accepting any hash
						$testPlayers = Array('Cartman', 'Kyle', 'Stan','Kenny','Mr(s). Garrison', 'Mr. Mackey', 'Sharon', 'Mr. Slave', 'Token', 'Timmy', 'Jimmy', 'Butters','Clyde', 'Bebe', 'Wendy', 'Craig');
						$diffs = Array(1=>'easy',2=>'medium',3=>'hard');
						
						//generate some random scores
						for ($d=1;$d<=3;$d++) {
							$count = rand(1,20);
							for ($i=0;$i<$count;$i++) {
								array_push($scores[$diffs[$d]], Array(
									//player name (string)
									'Name'=>$testPlayers[rand(0,count($testPlayers)-1)],
									//player score (int)
									'Score'=>rand(500,9900),
									//if song is duet 
									'VoiceNr'=>rand(0,2), 
									//datetime as formatted string
									'date'=>strftime("%Y.%m.%d %H:%M", time()-(rand(0,10)*86400))
								));
							}
						}
						
						Response(Array('status'=>1, 'code'=>0, 'result'=>$scores));
						
					}else {
						Response(Array('status'=>0, 'code'=>12, 'message'=>'Custom error message. Vocaluxe can display it.'));
					}


				/*
				* Get available contests from server
				* @CommunityContest PartyMode is under development
				*/	
				
				}elseif ($request['method'] == 'getcontests') {
					
					
				}elseif ($request['method'] == 'getcontestplaylist') {
					
					
				}elseif ($request['method'] == 'getcontestaccess') {
					
				}
				else {
					Response(Array('status'=>0, 'code'=>3, 'message'=>'Wrong method!'));
				}
			}else {
				Response(Array('status'=>0, 'code'=>1, 'message'=>'Wrong request!'));
			}
		}else {
			Response(Array('status'=>0, 'code'=>1, 'message'=>'Wrong request!'));
		}
	
	}
	
	
	/*
	*	ERROR CODES
	*	0: Uknown error
	*	1: Wrong request
	*	2: Json parse error
	*	3: Method not implemented
	*	4: Wrong parameter count
	*	...
	*	10: Missing username/password (module need authentication)
	*	11: Auth error (wrong username or password)
	*	12: Missing txt hash
	*	13: Txt not found
	*	14: Wrong txt version (outdated txt hash)
	*/
	
	
	/*
	*	SONG IDENTIFICATION
	*	Vocaluxe now calculates md5 hash of TXT files before send/get scores
	*	Header lines and newlines are skipped!
	*	PHP TXT hash calculation sample below
	*/
	
	function hashTxt($file) {
		$data = file_get_contents($file);
		$datatohash = "";
		
		$nl = false;
		for ($i=0; $i<=strlen($data); $i++) {
			//newlines
			if (ord($data[$i]) == 10 || ord($data[$i]) == 13) {
				$nl 	   = true;
				$waitnext  = false;
			}else {
				//skip header lines 
				if ($nl == true && ord($data[$i]) == 35) {
					$waitnext = true;
				}
				//hashing only song data
				if ($waitnext == false) {
					$datatohash .= $data[$i];
				}
				$nl = false;
			}
		}
		return md5($datatohash);
	}
	
	exit();
